namespace Pos.SalesService.Infrastructure.Persistence.ReadModels;

public class SalesTaxRateReadModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public decimal Rate { get; set; }
    public bool IsActive { get; set; }
}
