namespace Pos.SalesService.Domain.Models;

public abstract class SalesEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
