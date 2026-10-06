using MediatR;
using Pos.SalesService.Application.Features.CashierShifts.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Application.Features.CashierShifts.Queries.GetSummaryQuery
{
    public class GetCashierShiftSummaryQuery : IRequest<Result<CashierShiftSummaryDto>>
    {
        public Guid ShiftId { get; set; }
    }

    public class GetCashierShiftSummaryQueryHandler: IRequestHandler<GetCashierShiftSummaryQuery, Result<CashierShiftSummaryDto>>
    {
        private readonly ICashierShiftRepositoryAsync _cashierShiftRepository;
        private readonly IRefundPaymentRepositoryAsync _refundPaymentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetCashierShiftSummaryQueryHandler(
            ICashierShiftRepositoryAsync cashierShiftRepository,
            IRefundPaymentRepositoryAsync refundPaymentRepository,
            ICurrentUserService currentUser)
        {
            _cashierShiftRepository = cashierShiftRepository;
            _refundPaymentRepository = refundPaymentRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<CashierShiftSummaryDto>> Handle(GetCashierShiftSummaryQuery request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var shift = await _cashierShiftRepository.GetCashierShiftByIdAsync(tenantId.Value,request.ShiftId,cancellationToken);

            if (shift == null)
                return Result<CashierShiftSummaryDto>.Failure(
                    "Shift was not found.");

            // 3. Calculate cash transactions belonging to this shift.
            var cashReceipts =
                await _cashierShiftRepository.CalculateShiftCashReceiptsAsync(
                    tenantId.Value, shift.Id, cancellationToken);

            var cashRefunds =
                await _refundPaymentRepository.CalculateShiftCashRefundsAsync(
                    tenantId.Value, shift.Id, cancellationToken);

            var totalSalesAmount =
                await _cashierShiftRepository.CalculateShiftTotalSalesAsync(
                    tenantId.Value, shift.Id, cancellationToken);

            var nonCashReceipts =
                await _cashierShiftRepository.CalculateShiftNonCashReceiptsAsync(
                    tenantId.Value, shift.Id, cancellationToken);

            // 4. Preserve the recorded reconciliation for closed shifts.
            var expectedCash = shift.Status == CashierShiftStatus.Closed
                ? shift.ExpectedCash
                : shift.OpeningCash + cashReceipts - cashRefunds;

            var summary = new CashierShiftSummaryDto
            {
                ShiftId = shift.Id,
                CashierUserId = shift.CashierUserId,
                BranchId = shift.BranchId,
                TerminalId = shift.TerminalId,
                Status = shift.Status,
                OpenedAt = shift.OpenedAt,
                ClosedAt = shift.ClosedAt,
                OpeningCash = shift.OpeningCash,
                CashReceipts = cashReceipts,
                CashRefunds = cashRefunds,
                TotalSalesAmount = totalSalesAmount,
                NonCashReceipts = nonCashReceipts,
                ExpectedCash = expectedCash,
                ClosingCash = shift.ClosingCash,
                Difference = shift.Difference
            };

            return Result<CashierShiftSummaryDto>.Success(summary);
        }
    }
}