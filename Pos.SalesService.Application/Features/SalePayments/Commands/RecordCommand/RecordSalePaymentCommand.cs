using MediatR;
using Pos.SalesService.Application.Features.SalePayments.Services;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.SalePayments.Commands.RecordCommand;

public class RecordSalePaymentCommand : IRequest<Result<Guid>>
{
    public Guid SaleId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class RecordSalePaymentCommandHandler : IRequestHandler<RecordSalePaymentCommand, Result<Guid>>
{
    private readonly ISalePaymentRepositoryAsync _paymentsRepository;
    private readonly IPaymentMethodRepositoryAsync _methods;
    private readonly SalePaymentWorkflow _workflow;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RecordSalePaymentCommandHandler(ISalePaymentRepositoryAsync payments,
        IPaymentMethodRepositoryAsync methods, SalePaymentWorkflow workflow,
        ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _paymentsRepository = payments;
        _methods = methods;
        _workflow = workflow;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RecordSalePaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Load the cashier's tenant-owned sale and recover duplicate requests first.
        var loadedSale = await _workflow.LoadSaleAsync(request.SaleId, cancellationToken);

        if (loadedSale.IsFailure)
            return Result<Guid>.Failure(loadedSale.Errors.ToArray());

        var sale = loadedSale.Value!;

        var reference = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber.Trim();

        var existing = await _paymentsRepository.GetByIdempotencyKeyAsync(sale.TenantId, request.IdempotencyKey, cancellationToken);

        if (existing != null)
            return ExistingResult(existing, request, reference);

        var state = _workflow.ValidateState(sale, request.RowVersion);

        if (state.IsFailure)
            return Result<Guid>.Failure(state.Errors.ToArray());

        var method = await _methods.GetPaymentMethodByIdAsync(sale.TenantId, request.PaymentMethodId, cancellationToken);

        if (method == null || !method.IsActive)
            return Result<Guid>.Failure("Select an active payment method.");

        if (method.RequiresReferenceNumber && reference == null)
            return Result<Guid>.Failure("This payment method requires a reference number.");

        // 2. Calculate change from successful payments only.
        var settlement = _workflow.Calculate(sale);

        if (settlement.IsFailure)
            return Result<Guid>.Failure(settlement.Errors.ToArray());

        var remainingDue = settlement.Value!.RemainingDue;

        if (remainingDue == 0)
            return Result<Guid>.Failure("This sale is already fully paid.");

        if (!method.IsCash && request.Amount > remainingDue)
            return Result<Guid>.Failure("A non-cash payment cannot exceed the remaining amount due.");

        // 3. Every payment completes immediately — the cashier confirming it IS the
        // confirmation, since the terminal is a separate, unintegrated device with
        // no callback to wait for. Only validate the reservation on the payment
        // that actually finalizes the sale; a partial tender can't trigger an
        // oversell on its own, since nothing is finalized until the balance hits zero.

        var changeAmount = method.IsCash ? Math.Max(request.Amount - remainingDue, 0) : 0;

        var amountAppliedToSale = request.Amount - changeAmount;

        var coversRemainingAmountDue = amountAppliedToSale >= remainingDue;

        if (coversRemainingAmountDue)
        {
            var reservation = await _workflow.ValidateReservationAsync(
                sale,
                cancellationToken);

            if (reservation.IsFailure)
                return Result<Guid>.Failure(reservation.Errors.ToArray());
        }

        var userId = Guid.Parse(_currentUser.UserId!);
        var now = DateTime.UtcNow;

        var payment = new SalePayment
        {
            Id = Guid.NewGuid(),
            TenantId = sale.TenantId,
            SaleId = sale.Id,
            PaymentMethodId = method.Id,
            IdempotencyKey = request.IdempotencyKey,
            PaymentMethodNameSnapshot = method.Name,
            PaymentMethodCodeSnapshot = method.Code,
            IsCashSnapshot = method.IsCash,
            Amount = request.Amount,
            ChangeAmount = method.IsCash ? Math.Max(request.Amount - remainingDue, 0) : 0,
            ReferenceNumber = reference,
            Status = PaymentStatus.Completed,
            ReceivedByUserId = userId,
            ConfirmedByUserId = userId,
            CreatedAt = now,
            PaidAt = now
        };

        sale.Payments.Add(payment);

        await _paymentsRepository.AddAsync(payment, cancellationToken);

        var totals = _workflow.UpdateTotals(sale);

        if (totals.IsFailure)
            return Result<Guid>.Failure(totals.Errors.ToArray());

        // 4. Save payment and settlement in one transaction, protected by the sale row version.
        var saved = await _unitOfWork.TrySavePaymentChangesAsync(cancellationToken);

        if (saved.IsFailure)
        {
            var existingPayment = await _paymentsRepository.GetByIdempotencyKeyAsync(
                sale.TenantId, request.IdempotencyKey, cancellationToken);

            if (existingPayment != null)
                return ExistingResult(existingPayment, request, reference);

            return Result<Guid>.Failure(saved.Errors.ToArray());
        }

        return Result<Guid>.Success(payment.Id);
    }

    private static Result<Guid> ExistingResult(SalePayment payment, RecordSalePaymentCommand request, string? reference)
    {
        if (payment.SaleId != request.SaleId || payment.PaymentMethodId != request.PaymentMethodId ||
            payment.Amount != request.Amount || (reference != null && payment.ReferenceNumber != reference))
            return Result<Guid>.Failure("This idempotency key was used for a different payment.");

        return Result<Guid>.Success(payment.Id);
    }
}