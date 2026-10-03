using MediatR;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Features.Sales.DTOs.Calculations;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.Sales.Commands.ApplyDiscountCommand
{
    public class ApplySaleDiscountCommand:IRequest<Result<Guid>>
    {
        public Guid SaleId { get; set; }

        public Guid? SaleItemId { get; set; }

        public string DiscountType { get; set; } = string.Empty;

        public decimal Value { get; set; }

        public string Reason { get; set; } = string.Empty;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
    public class ApplySaleDiscountCommandHandler : IRequestHandler<ApplySaleDiscountCommand, Result<Guid>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ISaleCalculationService _saleCalculationService;
        private readonly IUnitOfWork _unitOfWork;

        public ApplySaleDiscountCommandHandler(
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
            ApplySaleDiscountCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Require the authenticated tenant and user.
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "A valid tenant is required.");

            if (!Guid.TryParse(_currentUser.UserId, out var userId) ||
                userId == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "A valid user is required.");

            // 2. Load the sale with its items, discounts and shift.
            var sale = await _saleRepository.GetByIdAsync(
                tenantId.Value,
                request.SaleId,
                cancellationToken);

            if (sale == null)
                return Result<Guid>.Failure("Sale was not found.");

            if (sale.Status != SaleStatus.Draft)
                return Result<Guid>.Failure(
                    "Discounts can only be applied to draft sales.");

            if (!sale.RowVersion.SequenceEqual(request.RowVersion))
                return Result<Guid>.Failure(
                    "This sale has changed. Reload it before applying a discount.");

            if (sale.Shift.Status != CashierShiftStatus.Open ||
                sale.Shift.CashierUserId != userId)
                return Result<Guid>.Failure(
                    "The sale must belong to your open shift.");

            if (sale.Items.Count == 0)
                return Result<Guid>.Failure(
                    "The sale must contain at least one item.");

            // 3. Ensure an item discount targets an item in this sale.
            if (request.SaleItemId.HasValue &&
                !sale.Items.Any(i => i.Id == request.SaleItemId.Value))
                return Result<Guid>.Failure(
                    "The selected item does not belong to this sale.");

            // 4. Prepare the discount without changing tracked entities yet.
            var newDiscount = new SaleDiscount
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                SaleId = sale.Id,
                SaleItemId = request.SaleItemId,
                DiscountType = request.DiscountType,
                Value = request.Value,
                Reason = request.Reason.Trim(),
                CreatedByUserId = userId,
                ApprovedByUserId = null,
                CreatedAt = DateTime.UtcNow
            };

            // Use a stable order for all discounts, including the new one.
            var discounts = sale.Discounts
                .Append(newDiscount)
                .OrderBy(d => d.CreatedAt)
                .ToList();

            // 5. Recalculate from original quantities/prices and all discounts.
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

                Discounts = discounts
                    .Select((discount, index) =>
                        new SaleDiscountCalculationInput
                        {
                            DiscountId = discount.Id,
                            SaleItemId = discount.SaleItemId,
                            DiscountType = discount.DiscountType,
                            Value = discount.Value,
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

            // 6. Record the new discount and refresh every discount's amount.
            sale.Discounts.Add(newDiscount);

            var calculatedDiscounts = calculatedSale.Discounts
                .ToDictionary(d => d.DiscountId);

            foreach (var discount in discounts)
            {
                discount.Amount =
                    calculatedDiscounts[discount.Id].Amount;
            }

            // 7. Update calculated item amounts and sale totals.
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

            // 8. Save the discount.
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(
                    "This sale changed while saving. Reload it and try again.");
            }

            return Result<Guid>.Success(newDiscount.Id);
        }
    }
}
