using MediatR;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Features.Sales.DTOs.Calculations;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Application.Features.Sales.Commands.RemoveDiscountCommand
{
    public class RemoveSaleDiscountCommand : IRequest<Result<Guid>>
    {
        public Guid SaleId { get; set; }
        public Guid DiscountId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class RemoveSaleDiscountCommandHandler
        : IRequestHandler<RemoveSaleDiscountCommand, Result<Guid>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ISaleCalculationService _saleCalculationService;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveSaleDiscountCommandHandler(
            ISaleRepositoryAsync saleRepository,
            ICurrentUserService currentUser,
            ISaleCalculationService saleCalculationService,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _currentUser = currentUser;
            _saleCalculationService = saleCalculationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(
            RemoveSaleDiscountCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Require the authenticated tenant and cashier.
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "A valid tenant is required.");

            if (!Guid.TryParse(_currentUser.UserId, out var userId) ||
                userId == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "A valid user is required.");

            // 2. Load the sale and check its status, version and shift.
            var sale = await _saleRepository.GetByIdAsync(
                tenantId.Value,
                request.SaleId,
                cancellationToken);

            if (sale == null)
                return Result<Guid>.Failure("Sale was not found.");

            if (sale.Status != SaleStatus.Draft)
                return Result<Guid>.Failure(
                    "Discounts can only be removed from draft sales.");

            if (!sale.RowVersion.SequenceEqual(request.RowVersion))
                return Result<Guid>.Failure(
                    "This sale has changed. Reload it before removing a discount.");

            if (sale.Shift.Status != CashierShiftStatus.Open ||
                sale.Shift.CashierUserId != userId)
                return Result<Guid>.Failure(
                    "The sale must belong to your open shift.");

            // 3. Find the discount within this sale.
            var discount = sale.Discounts
                .SingleOrDefault(d => d.Id == request.DiscountId);

            if (discount == null)
                return Result<Guid>.Failure(
                    "The discount was not found in this sale.");

            var remainingDiscounts = sale.Discounts
                .Where(d => d.Id != request.DiscountId)
                .OrderBy(d => d.CreatedAt)
                .ToList();

            // 4. Recalculate from original prices using only remaining discounts.
            var calculationInput = new SaleCalculationInput
            {
                PricesIncludeTax = sale.PricesIncludeTax,

                Items = sale.Items
                    .OrderBy(i => i.ItemNumber)
                    .Select(item => new SaleItemCalculationInput
                    {
                        SaleItemId = item.Id,
                        ItemNumber = item.ItemNumber,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TaxRate = item.TaxRate
                    })
                    .ToList(),

                Discounts = remainingDiscounts
                    .Select((item, index) =>
                        new SaleDiscountCalculationInput
                        {
                            DiscountId = item.Id,
                            SaleItemId = item.SaleItemId,
                            DiscountType = item.DiscountType,
                            Value = item.Value,
                            ApplicationOrder = index
                        })
                    .ToList()
            };

            var calculationResult =
                _saleCalculationService.CalculateSale(calculationInput);

            if (calculationResult.IsFailure)
                return Result<Guid>.Failure(
                    calculationResult.Errors.ToArray());

            var calculatedSale = calculationResult.Value!;

            // 5. Stage deletion and refresh remaining discount amounts.
            _saleRepository.RemoveDiscount(sale, discount);

            var calculatedDiscounts = calculatedSale.Discounts
                .ToDictionary(d => d.DiscountId);

            foreach (var remainingDiscount in remainingDiscounts)
            {
                remainingDiscount.Amount =
                    calculatedDiscounts[remainingDiscount.Id].Amount;
            }

            // 6. Update item amounts and sale totals.
            var calculatedItems = calculatedSale.Items
                .ToDictionary(i => i.SaleItemId);

            foreach (var item in sale.Items)
            {
                var calculatedItem = calculatedItems[item.Id];

                item.DiscountAmount = calculatedItem.DiscountAmount;
                item.TaxAmount = calculatedItem.TaxAmount;
                item.LineTotal = calculatedItem.LineTotal;
            }

            sale.Subtotal = calculatedSale.Subtotal;
            sale.DiscountTotal = calculatedSale.DiscountTotal;
            sale.TaxTotal = calculatedSale.TaxTotal;
            sale.Total = calculatedSale.Total;
            sale.UpdatedAt = DateTime.UtcNow;

            // 7. Save deletion and recalculated amounts together.
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(
                    "This sale changed while saving. Reload it and try again.");
            }

            return Result<Guid>.Success(sale.Id);
        }
    }
}
