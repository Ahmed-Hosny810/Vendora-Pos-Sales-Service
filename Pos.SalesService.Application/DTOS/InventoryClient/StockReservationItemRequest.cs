
namespace Pos.SalesService.Application.DTOS.InventoryClient
{
    public class StockReservationItemRequest
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public decimal Quantity { get; set; }
    }
}
