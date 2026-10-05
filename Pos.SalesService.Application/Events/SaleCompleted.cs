
namespace Pos.SalesService.Application.Events
{
     public record SaleCompleted(
         Guid EventId,
         Guid TenantId,
         Guid SaleId,
         Guid TerminalId,
         Guid BranchId,
         Guid? StockReservationId,
         Guid CashierUserId,
         string ReceiptNumber,
         decimal Subtotal,
         decimal DiscountTotal,
         decimal TaxTotal,
         decimal Total,
         List<SaleCompletedItem> Items,
         DateTime OccurredAt);

     public record SaleCompletedItem(
         Guid ProductId,
         Guid? ProductVariantId,
         decimal Quantity,
         decimal UnitPrice,
         decimal UnitCost,
         decimal DiscountAmount,
         decimal TaxAmount,
         decimal LineTotal,
         bool TrackInventory);



}
