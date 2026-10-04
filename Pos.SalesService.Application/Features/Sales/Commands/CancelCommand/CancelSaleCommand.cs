using MediatR;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Clients;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.Sales.Commands.CancelCommand
{
    public class CancelSaleCommand : IRequest<Result<Guid>>
    {
        public Guid SaleId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Reason { get; set; } = string.Empty;
    }

    public class CancelSaleCommandHandler : IRequestHandler<CancelSaleCommand, Result<Guid>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly IInventoryClient _inventoryClient;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public CancelSaleCommandHandler(
            ISaleRepositoryAsync saleRepository,
            IInventoryClient inventoryClient,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _inventoryClient = inventoryClient;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
        {
            // 1. Require the authenticated tenant and cashier.
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            if (!Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("A valid user is required.");

            // 2. Load the sale.
            var sale = await _saleRepository.GetByIdAsync(tenantId.Value, request.SaleId, cancellationToken);

            if (sale == null)
                return Result<Guid>.Failure("Sale was not found.");

            if (sale.Shift.CashierUserId != userId)
                return Result<Guid>.Failure("The sale belongs to another cashier's shift.");

            // 3. Repeated cancellation is harmless and returns the same result.
            if (sale.Status == SaleStatus.Cancelled)
                return Result<Guid>.Success(sale.Id);

            // 4. Completed sales use returns; Completing is uncertain and must
            // be reconciled (via CompleteSaleCommand's retry) before it can be
            // judged either way — it must never be cancelled out from under
            // a completion that may have already succeeded on Inventory's side.
            if (sale.Status == SaleStatus.Completed)
                return Result<Guid>.Failure("This sale is completed. Use a return instead.");

            if (sale.Status == SaleStatus.Completing)
                return Result<Guid>.Failure(
                    "This sale's completion is unresolved. Retry completion to reconcile it before cancelling.");

            if (sale.Status != SaleStatus.Draft &&
                sale.Status != SaleStatus.CheckoutPending &&
                sale.Status != SaleStatus.PendingPayment)
                return Result<Guid>.Failure("This sale cannot be cancelled.");

            // 5. Any successful payment requires an explicit refund
            if (sale.Payments.Count > 0)
                return Result<Guid>.Failure(
                    "This sale has recorded payments. Void or refund them before cancelling.");

            if (!sale.RowVersion.SequenceEqual(request.RowVersion))
                return Result<Guid>.Failure("This sale has changed. Reload it before cancelling.");

            var now = DateTime.UtcNow;
            var oldStatus = sale.Status;

            // 6. A draft never reserved stock — cancel locally, no Inventory call.
            if (sale.Status == SaleStatus.Draft)
            {
                ApplyCancellation(sale, tenantId.Value, userId, oldStatus, request.Reason, now);

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (ConcurrencyConflictException)
                {
                    return Result<Guid>.Failure("This sale changed while cancelling. Reload it and try again.");
                }

                return Result<Guid>.Success(sale.Id);
            }

            // 7. CheckoutPending/PendingPayment: release the reservation through
            // Inventory first — idempotently, keyed by SaleId, so a retry after
            // a local save failure safely reconciles rather than erroring.
            // The irreversible local status change happens only after release
            // is confirmed, mirroring the ordering used for sale completion.
            if (sale.StockReservationId.HasValue)
            {
                var releaseResult = await _inventoryClient.ReleaseStockReservationAsync(sale.Id, cancellationToken);

                if (releaseResult.IsFailure)
                    return Result<Guid>.Failure(
                        releaseResult.Errors
                            .Concat(new[] { "Cancellation is unfinished. Retry to release the same reservation." })
                            .ToArray());
            }

            ApplyCancellation(sale, tenantId.Value, userId, oldStatus, request.Reason, now);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                // The reservation is already released on Inventory's side (or
                // was never active). Do not re-release here: another request
                // — including a concurrent completion — may have changed this
                // sale. Let the caller reload and re-evaluate from fresh state.
                return Result<Guid>.Failure(
                    "This sale changed while saving the cancellation. Reload it and retry.");
            }

            return Result<Guid>.Success(sale.Id);
        }

        private static void ApplyCancellation(
            Sale sale, Guid tenantId, Guid userId, string oldStatus, string reason, DateTime now)
        {
            sale.Status = SaleStatus.Cancelled;
            sale.CancelledAt = now;
            sale.UpdatedAt = now;

            sale.StatusHistory.Add(new SaleStatusHistory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SaleId = sale.Id,
                OldStatus = oldStatus,
                NewStatus = SaleStatus.Cancelled,
                ChangedByUserId = userId,
                ChangedAt = now,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Sale cancelled." : reason.Trim()
            });
        }
    }
}