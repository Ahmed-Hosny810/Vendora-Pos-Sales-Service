using MediatR;
using PhoneNumbers;
using Pos.SalesService.Application.DTOS;
using Pos.SalesService.Application.Features.Sales.DTOs;
using Pos.SalesService.Application.Features.Sales.DTOs.Calculations;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.Sales.Commands.CreateDraftCommand
{
    public class CreateSaleDraftCommand:IRequest<Result<Guid>>
    {
        public Guid? IdempotencyKey { get; set; }
        public string? CustomerPhone { get; set; }

        public List<SaleItemDto> Items { get; set; } = new();
    }

    public class CreateSaleDraftCommandHandler : IRequestHandler<CreateSaleDraftCommand, Result<Guid>>
    {
        private readonly ISaleRepositoryAsync _saleRepository;
        private readonly ICashierShiftRepositoryAsync _cashierShiftRepository;
        private readonly ICustomerRepositoryAsync _customerRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISalesValidationService _salesValidationService;
        private readonly ISaleCalculationService _saleCalculationService;

        public CreateSaleDraftCommandHandler(
            ISaleRepositoryAsync saleRepository,
            ICashierShiftRepositoryAsync cashierShiftRepository,
            ICustomerRepositoryAsync customerRepository,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            ISalesValidationService salesValidationService,
            ISaleCalculationService saleCalculationService
            )
        {
            _saleRepository = saleRepository;
            _cashierShiftRepository = cashierShiftRepository;
            _customerRepository = customerRepository;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _salesValidationService = salesValidationService;
            _saleCalculationService = saleCalculationService;
        }
        public async Task<Result<Guid>> Handle(CreateSaleDraftCommand request, CancellationToken cancellationToken)
        {
            // 1. Require the authenticated tenant and cashier.
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            if (!Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("A valid user is required.");

            if (!request.IdempotencyKey.HasValue || request.IdempotencyKey.Value == Guid.Empty)
                return Result<Guid>.Failure("A valid idempotency key is required.");

            // 2. Return the existing sale if exist.
            var existingSale = await _saleRepository.GetByIdempotencyKeyAsync(
                tenantId.Value,
                request.IdempotencyKey.Value,
                cancellationToken);

            if (existingSale != null)
                return Result<Guid>.Success(existingSale.Id);

            // 3. Resolve customer by phone.
            Customer? customer = null;

            if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                var phoneUtil = PhoneNumberUtil.GetInstance();
                string normalizedPhone;

                try
                {
                    var parsedPhone = phoneUtil.Parse(request.CustomerPhone, "EG");

                    if (!phoneUtil.IsValidNumber(parsedPhone) ||
                        parsedPhone.HasExtension)
                        return Result<Guid>.Failure(
                            "Enter a valid phone number without an extension.");

                    normalizedPhone = phoneUtil.Format(
                        parsedPhone,
                        PhoneNumberFormat.E164);
                }
                catch (NumberParseException)
                {
                    return Result<Guid>.Failure("Enter a valid phone number.");
                }

                customer = await _customerRepository.GetCustomerByPhoneAsync( tenantId.Value, normalizedPhone, cancellationToken);

                if (customer == null)
                    return Result<Guid>.Failure("Customer was not found.");

                if (!customer.IsActive)
                    return Result<Guid>.Failure("The customer is inactive.");
            }

            var openedShift = await _cashierShiftRepository.GetCurrentCashierShiftAsync(tenantId.Value, userId, cancellationToken);

            if (openedShift!.Status != CashierShiftStatus.Open)
                return Result<Guid>.Failure("An open cashier shift is required.");

            var terminalValidation = await _salesValidationService.ValidateTerminalAsync(openedShift.BranchId,openedShift.TerminalId,cancellationToken);

            if (terminalValidation.IsFailure)
                return Result<Guid>.Failure(terminalValidation.Errors.ToArray());

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                BranchId = openedShift.BranchId,
                TerminalId = openedShift.TerminalId,
                ShiftId = openedShift.Id,
                CreatedByUserId = userId,
                IdempotencyKey = request.IdempotencyKey.Value,
                Status = SaleStatus.Draft,

                CurrencyCode = "EGP",
                PricesIncludeTax = false,

                CustomerId = customer?.Id,
                CustomerNameSnapshot = customer?.FullName,
                CustomerPhoneSnapshot = customer?.Phone,
                CreatedAt = DateTime.UtcNow
            };

            // 6. Validate each product and capture trusted Catalog snapshots.

            var validationRequests = request.Items
                 .Select(i => new SaleItemValidationRequest
                 {
                     ProductId = i.ProductId,
                     ProductVariantId = i.ProductVariantId,
                     Quantity = i.Quantity
                 })
                 .ToList();

            var batchValidation = await _salesValidationService.ValidateSaleItemsAsync(validationRequests, cancellationToken);

            if (batchValidation.IsFailure)
                return Result<Guid>.Failure(batchValidation.Errors.ToArray());

            var validatedItemsByProductAndVariant = batchValidation.Value!
                .ToDictionary(v => (v.ProductId, v.ProductVariantId));

            var nextItemNumber = 1;

            foreach (var inputItem in request.Items)
            {
                var validatedItem = validatedItemsByProductAndVariant[(inputItem.ProductId, inputItem.ProductVariantId)];

                sale.Items.Add(new SaleItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    SaleId = sale.Id,
                    ItemNumber = nextItemNumber++,

                    ProductId = validatedItem.ProductId,
                    ProductVariantId = validatedItem.ProductVariantId,
                    ProductNameSnapshot = validatedItem.ProductName,
                    VariantNameSnapshot = validatedItem.VariantName,
                    SkuSnapshot = validatedItem.Sku,
                    BarcodeSnapshot = validatedItem.Barcode,
                    UnitNameSnapshot = validatedItem.UnitName,
                    TrackInventorySnapshot = validatedItem.TrackInventory,

                    Quantity = inputItem.Quantity,
                    UnitPrice = validatedItem.UnitPrice,
                    UnitCost = validatedItem.UnitCost,
                    TaxRate = validatedItem.TaxRate
                });
            }

            // 7. Calculate prices using the saved snapshots, not frontend totals.
            var calculationInput = new SaleCalculationInput
            {
                PricesIncludeTax = sale.PricesIncludeTax,
                Items = sale.Items.Select(item => new SaleItemCalculationInput
                {
                    SaleItemId = item.Id,
                    ItemNumber = item.ItemNumber,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = item.TaxRate
                }).ToList()
            };

            var calculationResult = _saleCalculationService.CalculateSale(calculationInput);

            if (calculationResult.IsFailure)
                return Result<Guid>.Failure(calculationResult.Errors.ToArray());

            var calculatedSale = calculationResult.Value!;

            // 8. Apply the calculated amounts to the sale and its items.
            var calculatedItemsById = calculatedSale.Items.ToDictionary(item => item.SaleItemId);

            foreach (var item in sale.Items)
            {
                var calculatedItem = calculatedItemsById[item.Id];

                item.DiscountAmount = calculatedItem.DiscountAmount;
                item.TaxAmount = calculatedItem.TaxAmount;
                item.LineTotal = calculatedItem.LineTotal;
            }

            sale.Subtotal = calculatedSale.Subtotal;
            sale.DiscountTotal = calculatedSale.DiscountTotal;
            sale.TaxTotal = calculatedSale.TaxTotal;
            sale.Total = calculatedSale.Total;

            // 9. Save the sale.

            await _saleRepository.AddAsync(sale, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(sale.Id);
        }
    }
}
