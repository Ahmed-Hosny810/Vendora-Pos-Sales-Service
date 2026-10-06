using MediatR;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Features.CashierShifts.DTOs;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Application.Features.CashierShifts.Commands.CloseShiftCommand
{
    public class CloseCashierShiftCommand:IRequest<Result<CloseCashierShiftResult>>
    {
        public decimal ClosingCash { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class CloseCashierShiftCommandHandler : IRequestHandler<CloseCashierShiftCommand, Result<CloseCashierShiftResult>>
    {
        private readonly ICashierShiftRepositoryAsync _cashierShiftRepository;
        private readonly IRefundPaymentRepositoryAsync _refundPaymentRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public CloseCashierShiftCommandHandler(
            ICashierShiftRepositoryAsync cashierShiftRepository,
            IRefundPaymentRepositoryAsync refundPaymentRepository,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork
            )
        {
            _cashierShiftRepository = cashierShiftRepository;
            _refundPaymentRepository = refundPaymentRepository;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<CloseCashierShiftResult>> Handle(CloseCashierShiftCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "A valid tenant is required.");

            if (!Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("A valid user is required.");


            // 2. Load the cashier's current open shift
            var shift = await _cashierShiftRepository.GetCurrentCashierShiftAsync(tenantId.Value,userId,cancellationToken);

            if (shift == null)
                return Result<CloseCashierShiftResult>.Failure("Shift not found");

            if (!shift.RowVersion.SequenceEqual(request.RowVersion))
                return Result<CloseCashierShiftResult>.Failure("This shift has changed. Reload it before completing.");

            // 3. Require unfinished operations to be resolved.

            if (!(await _cashierShiftRepository.CanShiftBeClosedAsync(tenantId.Value,shift.Id,cancellationToken)))
                return Result<CloseCashierShiftResult>
                    .Failure("Finish your pending operations before closing your shift.");

            // 4. Calculate the expected cash and the counted difference.
            var cashReceipts =await _cashierShiftRepository.CalculateShiftCashReceiptsAsync(tenantId.Value, shift.Id, cancellationToken);

            var cashRefunds =await _refundPaymentRepository.CalculateShiftCashRefundsAsync(tenantId.Value, shift.Id, cancellationToken);

            var expectedCash = shift.OpeningCash + cashReceipts - cashRefunds;
            var difference = request.ClosingCash - expectedCash;

            // 5. Record the closing amounts, actor and time.
            var now = DateTime.UtcNow;

            shift.ClosingCash = request.ClosingCash;
            shift.ExpectedCash = expectedCash;
            shift.Difference = difference;
            shift.Status = CashierShiftStatus.Closed;
            shift.ClosedByUserId = userId;
            shift.ClosedAt = now;
            shift.UpdatedAt = now;

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<CloseCashierShiftResult>.Failure(
                    "This shift changed while closing. Reload the summary and try again.");
            }

            return Result<CloseCashierShiftResult>.Success(new CloseCashierShiftResult
                  {
                      ShiftId = shift.Id,
                      OpeningCash = shift.OpeningCash,
                      ExpectedCash = expectedCash,
                      ClosingCash = request.ClosingCash,
                      Difference = difference,
                      ClosedAt = now
                  });
        }
    }
}
