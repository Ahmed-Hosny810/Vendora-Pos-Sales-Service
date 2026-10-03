using MediatR;
using Pos.SalesService.Application.DTOS.InventoryClient;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Clients;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.Sales.Commands.SaleCheckoutCommand
{
    public class StartSaleCheckoutCommand : IRequest<Result<Guid>>
    {
        public Guid SaleId { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class StartSaleCheckoutCommandHandler : IRequestHandler<StartSaleCheckoutCommand, Result<Guid>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly IInventoryClient _inventoryClient;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public StartSaleCheckoutCommandHandler(
            ISaleRepositoryAsync saleRepository,
            IInventoryClient inventoryClient,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork
            )
        {
            _saleRepository = saleRepository;
            _inventoryClient = inventoryClient;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<Guid>> Handle(
            StartSaleCheckoutCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Require the authenticated tenant and cashier.
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            if (!Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("A valid user is required.");

            // 2. Load the sale and require the cashier's open shift.
            var sale = await _saleRepository.GetByIdAsync(tenantId.Value, request.SaleId, cancellationToken);

            if (sale == null)
                return Result<Guid>.Failure("Sale was not found.");

            if (sale.Shift.Status != CashierShiftStatus.Open || sale.Shift.CashierUserId != userId)
                return Result<Guid>.Failure("The sale must belong to your open shift.");

            if (sale.Status != SaleStatus.Draft &&
                sale.Status != SaleStatus.CheckoutPending &&
                sale.Status != SaleStatus.PendingPayment)
                return Result<Guid>.Failure("This sale cannot start checkout.");

            if (sale.Items.Count == 0)
                return Result<Guid>.Failure("The sale must contain at least one item.");

            var reservationItems = sale.Items
                .Where(i => i.TrackInventorySnapshot)
                .Select(i => new StockReservationItemRequest
                {
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Quantity = i.Quantity
                })
                .ToList();

            // 3. Lock a new checkout before making an external request.
            // Retries resume the saved checkout without requiring the old
            // Draft row version to match the newer checkout row version.
            if (sale.Status == SaleStatus.Draft)
            {
                if (!sale.RowVersion.SequenceEqual(request.RowVersion))
                    return Result<Guid>.Failure("This sale has changed. Reload it before checkout.");

                var now = DateTime.UtcNow;

                sale.Status = SaleStatus.CheckoutPending;
                sale.UpdatedAt = now;

                sale.StatusHistory.Add(new SaleStatusHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    SaleId = sale.Id,
                    OldStatus = SaleStatus.Draft,
                    NewStatus = SaleStatus.CheckoutPending,
                    ChangedByUserId = userId,
                    ChangedAt = now,
                    Reason = "Checkout started."
                });

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (ConcurrencyConflictException)
                {
                    return Result<Guid>.Failure(
                        "This sale changed while starting checkout. Reload it and try again.");
                }
            }

            // 4. Create the reservation for tracked items. The idempotency key
            // (the sale's own ID) guarantees a retry of this call returns the same reservation
            Guid? reservationId = sale.StockReservationId;

            if (reservationItems.Count > 0)
            {
                if (!reservationId.HasValue)
                {
                    // PendingPayment without a reservation is inconsistent.
                    if (sale.Status == SaleStatus.PendingPayment)
                        return Result<Guid>.Failure("This sale is missing its stock reservation.");

                    var reservationResult = await _inventoryClient.CreateStockReservationAsync(
                        new CreateStockReservationRequest
                        {
                            SaleId = sale.Id,
                            BranchId = sale.BranchId,
                            Items = reservationItems
                        },
                        cancellationToken);

                    if (reservationResult.IsFailure)
                        return Result<Guid>.Failure(reservationResult.Errors.ToArray());

                    if (reservationResult.Value == Guid.Empty)
                        return Result<Guid>.Failure("Inventory returned an invalid reservation ID.");

                    reservationId = reservationResult.Value;
                }
            }
            else if (reservationId.HasValue)
            {
                return Result<Guid>.Failure("A sale without tracked items has an unexpected reservation.");
            }

            // 5. A repeated successful checkout needs no second transition.
            if (sale.Status == SaleStatus.PendingPayment)
                return Result<Guid>.Success(sale.Id);

            // 6. Record the reservation and transition to payment.
            var transitionTime = DateTime.UtcNow;

            sale.StockReservationId = reservationId;
            sale.Status = SaleStatus.PendingPayment;
            sale.UpdatedAt = transitionTime;

            sale.StatusHistory.Add(new SaleStatusHistory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                SaleId = sale.Id,
                OldStatus = SaleStatus.CheckoutPending,
                NewStatus = SaleStatus.PendingPayment,
                ChangedByUserId = userId,
                ChangedAt = transitionTime,
                Reason = "Checkout is ready for payment."
            });

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(
                    "This sale changed while saving checkout. Reload it and retry to recover the reservation.");
            }

            return Result<Guid>.Success(sale.Id);
        }
    }
}
