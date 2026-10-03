
namespace Pos.SalesService.Application.DTOS.InventoryClient
{
    public class CreateStockReservationRequest
    {
        public Guid SaleId { get; set; }
        public Guid BranchId { get; set; }

        public List<StockReservationItemRequest> Items { get; set; } = new();
    }
}
