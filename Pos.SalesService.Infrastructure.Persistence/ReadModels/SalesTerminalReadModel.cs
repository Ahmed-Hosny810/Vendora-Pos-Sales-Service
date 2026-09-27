namespace Pos.SalesService.Infrastructure.Persistence.ReadModels;

public class SalesTerminalReadModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string Status { get; set; } = string.Empty;
}

