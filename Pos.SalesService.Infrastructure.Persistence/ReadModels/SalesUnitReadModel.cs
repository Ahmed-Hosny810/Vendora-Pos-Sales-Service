namespace Pos.SalesService.Infrastructure.Persistence.ReadModels;

public class SalesUnitReadModel
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public bool IsDecimalAllowed { get; set; }
    public string Name { get; set; } = string.Empty;
}
