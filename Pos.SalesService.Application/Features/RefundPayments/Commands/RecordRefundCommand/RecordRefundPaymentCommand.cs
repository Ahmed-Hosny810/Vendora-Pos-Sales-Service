using MediatR;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.RefundPayments.Commands.RecordRefundCommand
{
    public class RecordRefundPaymentCommand : IRequest<Result<Guid>>
    {
        public Guid ReturnId { get; set; }
        public Guid PaymentMethodId { get; set; }
        public Guid IdempotencyKey { get; set; }
        public string? ReferenceNumber { get; set; }
    }

    public class RecordRefundPaymentCommandHandler
        : IRequestHandler<RecordRefundPaymentCommand, Result<Guid>>
    {
        private readonly IRefundPaymentRepositoryAsync _refundRepository;
        private readonly ICashierShiftRepositoryAsync _shiftRepository;
        private readonly IPaymentMethodRepositoryAsync _paymentMethodRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public RecordRefundPaymentCommandHandler(
            IRefundPaymentRepositoryAsync refundRepository,
            ICashierShiftRepositoryAsync shiftRepository,
            IPaymentMethodRepositoryAsync paymentMethodRepository,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork)
        {
            _refundRepository = refundRepository;
            _shiftRepository = shiftRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(
            RecordRefundPaymentCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Require the authenticated tenant and paying cashier.
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty ||
                !Guid.TryParse(_currentUser.UserId, out var userId) ||
                userId == Guid.Empty)
            {
                return Result<Guid>.Failure(
                    "A valid authenticated tenant user is required.");
            }

            //for non-cash
            var referenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
                ? null
                : request.ReferenceNumber.Trim();

            // 2. Return the existing payment when the same request is retried.
            var existingPayment = await _refundRepository.GetByIdempotencyKeyAsync(
                tenantId.Value,
                request.IdempotencyKey,
                cancellationToken);

            if (existingPayment != null)
                return CheckExistingPayment(existingPayment, request, referenceNumber);

            // 3. Load the accepted return and its refund payments.
            var saleReturn = await _refundRepository.GetReturnForRefundAsync(
                tenantId.Value,
                request.ReturnId,
                cancellationToken);

            if (saleReturn == null)
                return Result<Guid>.Failure("Sale return was not found.");

            // 4. Extract the amount payed back to the customer to calculate remaining .
            var refundedAmount = saleReturn.RefundPayments
                .Where(payment => payment.Status == PaymentStatus.Completed)
                .Sum(payment => payment.Amount);

            var amountToRefund = saleReturn.RefundAmount - refundedAmount;

            if (amountToRefund <= 0)
            {
                return Result<Guid>.Failure(
                    "There is no remaining amount to refund.");
            }

            // 5. Require the cashier's open paying shift in the return's branch.
            var shift = await _shiftRepository.GetCurrentCashierShiftAsync(
                tenantId.Value,
                userId,
                cancellationToken);

            if (shift == null || shift.Status != CashierShiftStatus.Open)
                return Result<Guid>.Failure("An open cashier shift is required.");

            if (shift.BranchId != saleReturn.BranchId)
            {
                return Result<Guid>.Failure(
                    "The paying shift must belong to the return's branch.");
            }

            // 6. Validate the selected method and its required reference.
            var method = await _paymentMethodRepository.GetPaymentMethodByIdAsync(
                tenantId.Value,
                request.PaymentMethodId,
                cancellationToken);

            if (method == null || !method.IsActive)
                return Result<Guid>.Failure("Select an active payment method.");

            //if (method.RequiresReferenceNumber && referenceNumber == null)
            //{
            //    return Result<Guid>.Failure(
            //        "A reference number is required for this payment method.");
            //}

            // 7. Record the full refund verified by the cashier.
            var now = DateTime.UtcNow;

            var refundPayment = new RefundPayment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                ReturnId = saleReturn.Id,
                ShiftId = shift.Id,
                PaymentMethodId = method.Id,
                IdempotencyKey = request.IdempotencyKey,

                PaymentMethodNameSnapshot = method.Name,
                PaymentMethodCodeSnapshot = method.Code,
                IsCashSnapshot = method.IsCash,

                Amount = amountToRefund,
                ReferenceNumber = referenceNumber,
                Status = PaymentStatus.Completed,
                PaidByUserId = userId,
                CreatedAt = now,
                PaidAt = now
            };

            // Check the return and shift versions when saving.
            _refundRepository.MarkReturnAndShiftForUpdate(saleReturn, shift);

            await _refundRepository.AddAsync(refundPayment, cancellationToken);

            // 8. Save atomically and check whether a concurrent retry succeeded.
            var saveResult = await _unitOfWork.TrySaveRefundChangesAsync(
                cancellationToken);

            if (saveResult.IsFailure)
            {
                existingPayment = await _refundRepository.GetByIdempotencyKeyAsync(
                    tenantId.Value,
                    request.IdempotencyKey,
                    cancellationToken);

                if (existingPayment != null)
                {
                    return CheckExistingPayment(
                        existingPayment,
                        request,
                        referenceNumber);
                }

                return Result<Guid>.Failure(saveResult.Errors.ToArray());
            }

            return Result<Guid>.Success(refundPayment.Id);
        }

        private static Result<Guid> CheckExistingPayment(
            RefundPayment existing,
            RecordRefundPaymentCommand request,
            string? referenceNumber)
        {
            if (existing.ReturnId != request.ReturnId ||
                existing.PaymentMethodId != request.PaymentMethodId ||
                existing.ReferenceNumber != referenceNumber)
            {
                return Result<Guid>.Failure(
                    "The idempotency key was already used for a different refund payment.");
            }

            if (existing.Status != PaymentStatus.Completed)
            {
                return Result<Guid>.Failure(
                    "This refund request already exists but is not completed. " +
                    "Review it before paying again.");
            }

            return Result<Guid>.Success(existing.Id);
        }
    }
}