
namespace Pos.SalesService.Application.DTOS.InventoryClient
{
    public class StockReservationDetailsDto
    {
        public Guid Id { get; set; }

        // The sale ID used when creating the reservation.
        public Guid ReferenceId { get; set; }

        public Guid BranchId { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public List<StockReservationItemDetailsDto> Items { get; set; } = new();
    }

    public class StockReservationItemDetailsDto
    {
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public decimal Quantity { get; set; }
    }
}
