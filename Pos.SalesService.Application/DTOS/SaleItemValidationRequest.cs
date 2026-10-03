
namespace Pos.SalesService.Application.DTOS
{
    public class SaleItemValidationRequest
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public decimal Quantity { get; set; }
    }
}
