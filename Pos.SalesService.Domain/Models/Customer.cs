using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class Customer : SalesEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? Area { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
