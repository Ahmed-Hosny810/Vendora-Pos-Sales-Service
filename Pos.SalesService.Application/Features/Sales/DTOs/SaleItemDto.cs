
namespace Pos.SalesService.Application.Features.Sales.DTOs
{
    public class SaleItemDto
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public decimal Quantity { get; set; }
    }
}
