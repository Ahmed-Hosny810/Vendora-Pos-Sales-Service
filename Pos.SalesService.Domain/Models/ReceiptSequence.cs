using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class ReceiptSequence : SalesEntity
{
    public Guid BranchId { get; set; }
    // Separate counters for sale receipts and return documents.
    public string DocumentType { get; set; } = ReceiptDocumentType.Sale;
    public string Prefix { get; set; } = string.Empty;
    public long LastNumber { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
