using MediatR;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Clients;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.Sales.Commands.CompleteCommand
{
    public class CompleteSaleCommand:IRequest<Result<Guid>>
    {
        public Guid SaleId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
    public class CompleteSaleCommandHandler
    : IRequestHandler<CompleteSaleCommand, Result<Guid>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly ISalePaymentRepositoryAsync _paymentRepository;
        private readonly IInventoryClient _inventoryClient;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public CompleteSaleCommandHandler(
            ISaleRepositoryAsync saleRepository,
            ISalePaymentRepositoryAsync paymentRepository,
            IInventoryClient inventoryClient,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _paymentRepository = paymentRepository;
            _inventoryClient = inventoryClient;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(
            CompleteSaleCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Require the authenticated tenant and cashier.
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            if (!Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("A valid user is required.");

            var sale = await _paymentRepository.GetSaleForPaymentAsync(
                tenantId.Value, request.SaleId, cancellationToken);

            if (sale == null)
                return Result<Guid>.Failure("Sale was not found.");

            if (sale.Shift.CashierUserId != userId)
                return Result<Guid>.Failure("The sale belongs to another cashier's shift.");

            // 2. Repeated completion returns the existing sale and receipt.
            if (sale.Status == SaleStatus.Completed)
                return Result<Guid>.Success(sale.Id);

            if (sale.Status != SaleStatus.PendingPayment && sale.Status != SaleStatus.Completing)
                return Result<Guid>.Failure("This sale cannot be completed.");

            // Open-shift and client-version checks apply to starting completion.
            // An interrupted Completing sale resumes its previously saved intent.
            if (sale.Status == SaleStatus.PendingPayment)
            {
                if (sale.Shift.Status != CashierShiftStatus.Open)
                    return Result<Guid>.Failure("An open cashier shift is required.");

                if (!sale.RowVersion.SequenceEqual(request.RowVersion))
                    return Result<Guid>.Failure("This sale has changed. Reload it before completing.");
            }

            if (sale.Items.Count == 0)
                return Result<Guid>.Failure("The sale must contain at least one item.");

            // 3. Verify successful payments cover the sale exactly.
            var amountPaid = sale.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount - p.ChangeAmount);

            if (amountPaid != sale.Total)
                return Result<Guid>.Failure("The retained payment amount must equal the sale total.");

            if (sale.Payments.Any(p => p.Status == PaymentStatus.Pending))
                return Result<Guid>.Failure("Resolve pending payment attempts before completing.");

            var trackedItems = sale.Items.Where(i => i.TrackInventorySnapshot).ToList();

            if (trackedItems.Count > 0 && !sale.StockReservationId.HasValue)
                return Result<Guid>.Failure("The sale is missing its stock reservation.");

            if (trackedItems.Count == 0 && sale.StockReservationId.HasValue)
                return Result<Guid>.Failure("The sale has an unexpected stock reservation.");

            // 4. Persist completion intent before contacting Inventory.
            if (sale.Status == SaleStatus.PendingPayment)
            {
                var now = DateTime.UtcNow;

                sale.Status = SaleStatus.Completing;
                sale.UpdatedAt = now;

                sale.StatusHistory.Add(new SaleStatusHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    SaleId = sale.Id,
                    OldStatus = SaleStatus.PendingPayment,
                    NewStatus = SaleStatus.Completing,
                    ChangedByUserId = userId,
                    ChangedAt = now,
                    Reason = "Sale completion started."
                });

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (ConcurrencyConflictException)
                {
                    return Result<Guid>.Failure(
                        "This sale changed while starting completion. Reload it and retry.");
                }
            }

            // 5. Check the saved reservation and consume it only if still active.
            // This stays synchronous HTTP: the receipt (step 6) must only be issued
            // after consumption is confirmed, since a receipt number is irreversible
            // and must never be allocated before stock is actually settled.
            if (trackedItems.Count > 0)
            {
                var reservationResult = await _inventoryClient.GetStockReservationByIdAsync(
                    sale.StockReservationId!.Value, cancellationToken);

                if (reservationResult.IsFailure)
                    return Result<Guid>.Failure(reservationResult.Errors.ToArray());

                var reservation = reservationResult.Value!;

                if (reservation.Id != sale.StockReservationId.Value ||
                    reservation.ReferenceId != sale.Id ||
                    reservation.BranchId != sale.BranchId)
                    return Result<Guid>.Failure("The reservation does not match this sale.");

                if (reservation.Items == null ||
                    reservation.Items.Count != trackedItems.Count ||
                    trackedItems.Any(item =>
                        reservation.Items.Count(reserved =>
                            reserved.ProductId == item.ProductId &&
                            reserved.ProductVariantId == item.ProductVariantId &&
                            reserved.Quantity == item.Quantity) != 1))
                {
                    return Result<Guid>.Failure("Reserved items do not match this sale.");
                }

                // A previous attempt may already have consumed this reservation.
                if (reservation.Status != "Consumed")
                {
                    if (reservation.Status != "Active" || reservation.ExpiresAt <= DateTime.UtcNow)
                    {
                        return Result<Guid>.Failure(
                            "This paid sale no longer has an active reservation. " +
                            "Stock recovery or a refund is required. Do not collect payment again.");
                    }

                    var consumeResult = await _inventoryClient.ConsumeStockReservationAsync(
                        reservation.Id, cancellationToken);

                    if (consumeResult.IsFailure)
                    {
                        // Leave Completing saved. A timeout does not prove failure.
                        return Result<Guid>.Failure(
                            consumeResult.Errors
                                .Concat(new[]
                                {
                                "Completion is unfinished. Retry to check the " +
                                "same reservation; do not collect payment again."
                                })
                                .ToArray());
                    }
                }
            }

            // 6. Allocate the receipt, complete the sale and stage the SaleCompleted
            // event in one local save. Receipt retries do not repeat Inventory
            // consumption, and the event cannot be published without the sale
            // actually completing, since both commit in the same transaction.
            return await _saleRepository.IssueReceiptAsync(tenantId.Value, sale.Id, userId, cancellationToken);
        }
    }
}
