using MediatR;
using PhoneNumbers;
using Pos.SalesService.Application.DTOS;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Features.Sales.DTOs;
using Pos.SalesService.Application.Features.Sales.DTOs.Calculations;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.Sales.Commands.UpdateDraftCommand
{
    public class UpdateSaleDraftCommand : IRequest<Result<Guid>>
    {
        public Guid SaleId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string? CustomerPhone { get; set; }
        // Complete intended collection; omitted items are removed.
        public List<SaleItemDto> Items { get; set; } = new();
    }

    public class UpdateSaleDraftCommandHandler : IRequestHandler<UpdateSaleDraftCommand, Result<Guid>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly ICustomerRepositoryAsync _customerRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ISalesValidationService _salesValidationService;
        private readonly ISaleCalculationService _saleCalculationService;

        public UpdateSaleDraftCommandHandler(
            ISaleRepositoryAsync saleRepository,
            ICustomerRepositoryAsync customerRepository,
            ICurrentUserService currentUser,
            ISalesValidationService salesValidationService,
            ISaleCalculationService saleCalculationService)
        {
            _saleRepository = saleRepository;
            _customerRepository = customerRepository;
            _currentUser = currentUser;
            _salesValidationService = salesValidationService;
            _saleCalculationService = saleCalculationService;
        }

        public async Task<Result<Guid>> Handle(UpdateSaleDraftCommand request, CancellationToken cancellationToken)
        {
            // 1. Require the authenticated tenant and cashier.
            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            if (!Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("A valid user is required.");

            // 2. Load the tracked sale and reject stale or non-draft updates.
            var sale = await _saleRepository.GetByIdAsync(tenantId.Value, request.SaleId, cancellationToken);
            if (sale == null)
                return Result<Guid>.Failure("Sale was not found.");

            if (sale.Status != SaleStatus.Draft)
                return Result<Guid>.Failure("Only draft sales can be edited.");

            if (!sale.RowVersion.SequenceEqual(request.RowVersion))
                return Result<Guid>.Failure("This sale has changed. Reload it before editing.");

            if (sale.Shift.Status != CashierShiftStatus.Open || sale.Shift.CashierUserId != userId)
                return Result<Guid>.Failure("The sale must belong to your open shift.");

            var terminalValidation = await _salesValidationService.ValidateTerminalAsync(
                sale.BranchId, sale.TerminalId, cancellationToken);
            if (terminalValidation.IsFailure)
                return Result<Guid>.Failure(terminalValidation.Errors.ToArray());

            // Discount editing is a later feature. Do not silently discard or reorder existing discounts.
            if (sale.Discounts.Count != 0)
                return Result<Guid>.Failure("Remove the sale's discounts before editing its draft items.");

            // 3. Resolve the optional customer. Keep snapshots when the customer is unchanged.
            Customer? customer = null;
            if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                var phoneUtil = PhoneNumberUtil.GetInstance();
                string normalizedPhone;
                try
                {
                    var parsedPhone = phoneUtil.Parse(request.CustomerPhone, "EG");
                    if (!phoneUtil.IsValidNumber(parsedPhone) || parsedPhone.HasExtension)
                        return Result<Guid>.Failure("Enter a valid phone number without an extension.");
                    normalizedPhone = phoneUtil.Format(parsedPhone, PhoneNumberFormat.E164);
                }
                catch (NumberParseException)
                {
                    return Result<Guid>.Failure("Enter a valid phone number.");
                }

                customer = await _customerRepository.GetCustomerByPhoneAsync(
                    tenantId.Value, normalizedPhone, cancellationToken);
                if (customer == null)
                    return Result<Guid>.Failure("Customer was not found.");
                if (!customer.IsActive)
                    return Result<Guid>.Failure("The customer is inactive.");
            }

            // 4. Prepare items without modifying tracked entities before all validation succeeds.
            var existingItems = sale.Items.ToDictionary(i => (i.ProductId, i.ProductVariantId));

            var nextItemNumber = sale.Items
                .Select(i => i.ItemNumber)
                .DefaultIfEmpty(0)
                .Max();

            var validationRequests = request.Items
                .Select(i => new SaleItemValidationRequest
                {
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Quantity = i.Quantity
                })
                .ToList();

            var batchValidation = await _salesValidationService.ValidateSaleItemsAsync(
                validationRequests, cancellationToken);

            if (batchValidation.IsFailure)
                return Result<Guid>.Failure(batchValidation.Errors.ToArray());

            var validatedByProductAndVariant = batchValidation.Value!
                .ToDictionary(v => (v.ProductId, v.ProductVariantId));

            var preparedItems = new List<SaleItem>();

            foreach (var inputItem in request.Items)
            {
                var catalogItem = validatedByProductAndVariant[(inputItem.ProductId, inputItem.ProductVariantId)];

                existingItems.TryGetValue((inputItem.ProductId, inputItem.ProductVariantId), out var existingItem);

                preparedItems.Add(new SaleItem
                {
                    Id = existingItem?.Id ?? Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    SaleId = sale.Id,
                    ItemNumber = existingItem != null ? existingItem.ItemNumber : checked(++nextItemNumber),
                    ProductId = inputItem.ProductId,
                    ProductVariantId = inputItem.ProductVariantId,
                    Quantity = inputItem.Quantity,
                    ProductNameSnapshot = existingItem != null ? existingItem.ProductNameSnapshot : catalogItem.ProductName,
                    VariantNameSnapshot = existingItem != null ? existingItem.VariantNameSnapshot : catalogItem.VariantName,
                    SkuSnapshot = existingItem != null ? existingItem.SkuSnapshot : catalogItem.Sku,
                    BarcodeSnapshot = existingItem != null ? existingItem.BarcodeSnapshot : catalogItem.Barcode,
                    UnitNameSnapshot = existingItem != null ? existingItem.UnitNameSnapshot : catalogItem.UnitName,
                    TrackInventorySnapshot = existingItem != null ? existingItem.TrackInventorySnapshot : catalogItem.TrackInventory,
                    UnitPrice = existingItem != null ? existingItem.UnitPrice : catalogItem.UnitPrice,
                    UnitCost = existingItem != null ? existingItem.UnitCost : catalogItem.UnitCost,
                    TaxRate = existingItem != null ? existingItem.TaxRate : catalogItem.TaxRate
                });
            }

            // 5. Recalculate using preserved prices for existing items and current prices for new ones.
            var calculation = _saleCalculationService.CalculateSale(new SaleCalculationInput
            {
                PricesIncludeTax = sale.PricesIncludeTax,
                Items = preparedItems.Select(i => new SaleItemCalculationInput
                {
                    SaleItemId = i.Id,
                    ItemNumber = i.ItemNumber,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TaxRate = i.TaxRate
                }).ToList()
            });
            if (calculation.IsFailure)
                return Result<Guid>.Failure(calculation.Errors.ToArray());

            var totals = calculation.Value!;
            var calculatedItems = totals.Items.ToDictionary(i => i.SaleItemId);
            foreach (var item in preparedItems)
            {
                var calculatedItem = calculatedItems[item.Id];
                item.DiscountAmount = calculatedItem.DiscountAmount;
                item.TaxAmount = calculatedItem.TaxAmount;
                item.LineTotal = calculatedItem.LineTotal;
            }

            // 6. Update the sale's customer snapshots and totals.
            if (sale.CustomerId != customer?.Id)
            {
                sale.CustomerId = customer?.Id;
                sale.CustomerNameSnapshot = customer?.FullName;
                sale.CustomerPhoneSnapshot = customer?.Phone;
                sale.DeliveryAddressSnapshot = null;
            }
            sale.Subtotal = totals.Subtotal;
            sale.DiscountTotal = totals.DiscountTotal;
            sale.TaxTotal = totals.TaxTotal;
            sale.Total = totals.Total;
            sale.UpdatedAt = DateTime.UtcNow;

            // 7. Reconcile items and save atomically, including safe item-number swaps.
            try
            {
                await _saleRepository.SaveDraftAsync(sale, preparedItems, cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure("This sale changed while saving. Reload it and try again.");
            }

            return Result<Guid>.Success(sale.Id);
        }
    }
}