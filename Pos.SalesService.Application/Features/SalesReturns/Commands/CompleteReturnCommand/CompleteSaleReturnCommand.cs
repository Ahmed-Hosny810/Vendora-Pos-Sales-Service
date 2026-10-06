using MediatR;
using Pos.SalesService.Application.Events;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Features.SalesReturns.Services;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;
using System.Text.Json;

namespace Pos.SalesService.Application.Features.SalesReturns.Commands.CompleteReturnCommand
{
    public class CompleteSaleReturnCommand : IRequest<Result<Guid>>
    {
        public Guid OriginalSaleId { get; set; }
        public Guid IdempotencyKey { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<SaleReturnItemInput> Items { get; set; } = new();
    }

    public class CompleteSaleReturnCommandHandler : IRequestHandler<CompleteSaleReturnCommand, Result<Guid>>
    {
        private readonly ISaleReturnRepositoryAsync _saleReturnRepository;
        private readonly IOutboxRepositoryAsync _outboxRepository;
        private readonly SaleReturnService _saleReturnService;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public CompleteSaleReturnCommandHandler(
            ISaleReturnRepositoryAsync saleReturnRepository,
            IOutboxRepositoryAsync outboxRepository,
            SaleReturnService saleReturnService,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork
            )
        {
            _saleReturnRepository = saleReturnRepository;
            _outboxRepository = outboxRepository;
            _saleReturnService = saleReturnService;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<Guid>> Handle(CompleteSaleReturnCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty ||
                !Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
                return Result<Guid>.Failure("A valid authenticated tenant user is required.");

            // 1. A retry returns the existing return without consuming quantities again.
            var existingReturn = await _saleReturnRepository.GetByIdempotencyKeyAsync(
                tenantId.Value, request.IdempotencyKey, cancellationToken);
            if (existingReturn != null)
                return CheckExistingReturn(existingReturn, request);

            // 2. Load tracked original items, validate and calculate once.
            var prepared = await _saleReturnService.PrepareAsync(tenantId.Value, request.OriginalSaleId,
                Guid.NewGuid(), request.Items, cancellationToken);

            if (prepared.IsFailure)
                return Result<Guid>.Failure(prepared.Errors.ToArray());

            
            var saleReturn = prepared.Value!;

            saleReturn.Reason = request.Reason.Trim();
            saleReturn.IdempotencyKey = request.IdempotencyKey;
            saleReturn.ProcessedByUserId = userId;
            saleReturn.CompletedAt = DateTime.UtcNow;

            // 3. Record the returned quantities and the original sale's resulting status.
            var sale = saleReturn.OriginalSale;

            var originalSaleItems = sale.Items.ToDictionary(x => x.Id);

            foreach (var item in saleReturn.Items)
                originalSaleItems[item.OriginalSaleItemId].ReturnedQuantity += item.Quantity;

            var oldStatus = sale.Status;

            sale.Status = sale.Items.All(x => x.ReturnedQuantity == x.Quantity) ? SaleStatus.Returned : SaleStatus.PartiallyReturned;

            _saleReturnRepository.MarkOriginalSaleUpdated(sale);

            if (oldStatus != sale.Status)
            {
                sale.StatusHistory.Add(new SaleStatusHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    SaleId = sale.Id,
                    OldStatus = oldStatus,
                    NewStatus = sale.Status,
                    ChangedByUserId = userId,
                    ChangedAt = saleReturn.CompletedAt,
                    Reason = "Customer return completed."
                });
            }
            //Add SaleReturnCompleted Event to OutBoxMessages
            var eventId = Guid.NewGuid();

            var saleReturnCompleted = new SaleReturnCompleted(
                EventId: eventId,
                TenantId: tenantId.Value,
                ReturnId: saleReturn.Id,
                OriginalSaleId: saleReturn.OriginalSaleId,
                ReceivingBranchId: saleReturn.BranchId,
                Items: saleReturn.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new SaleReturnCompletedItem(
                        ReturnItemId: item.Id,
                        ProductId: item.ProductId,
                        ProductVariantId: item.ProductVariantId,
                        Quantity: item.Quantity,
                        Restock: item.Restock,
                        TrackInventory: originalSaleItems[item.OriginalSaleItemId].TrackInventorySnapshot,
                        StockCondition: item.StockCondition))
                    .ToList(),
                OccurredAt: saleReturn.CompletedAt);


            await _outboxRepository.AddAsync(
                 new OutboxMessage
                 {
                     Id = eventId,
                     TenantId = tenantId.Value,
                     EventType = nameof(SaleReturnCompleted),
                     Payload = JsonSerializer.Serialize(saleReturnCompleted),
                     OccurredAt = saleReturn.CompletedAt
                 },
                 cancellationToken);


            // 4. Commit the return, returned quantities, history and outbox event together.
            await _saleReturnRepository.AddAsync(saleReturn, cancellationToken);

            var saveResult = await _unitOfWork.TrySaveReturnChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                // The unit of work discarded failed tracked changes before this lookup.
                existingReturn = await _saleReturnRepository.GetByIdempotencyKeyAsync(
                    tenantId.Value, request.IdempotencyKey, cancellationToken);
                if (existingReturn != null)
                    return CheckExistingReturn(existingReturn, request);

                return Result<Guid>.Failure(saveResult.Errors.ToArray());
            }

            return Result<Guid>.Success(saleReturn.Id);
        }

        private static Result<Guid> CheckExistingReturn(SaleReturn existing, CompleteSaleReturnCommand request)
        {
            var sameRequest = existing.OriginalSaleId == request.OriginalSaleId &&
                existing.Reason == request.Reason.Trim() &&
                existing.Items.Count == request.Items.Count &&
                request.Items.All(input => existing.Items.Any(item =>
                    item.OriginalSaleItemId == input.OriginalSaleItemId &&
                    item.Quantity == input.Quantity &&
                    item.StockCondition == input.StockCondition &&
                    item.Restock == input.Restock));

            return sameRequest
                ? Result<Guid>.Success(existing.Id)
                : Result<Guid>.Failure("The idempotency key was already used for a different return request.");
        }
    }
}
