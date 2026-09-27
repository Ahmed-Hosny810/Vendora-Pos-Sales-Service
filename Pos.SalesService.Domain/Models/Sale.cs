using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class Sale : SalesEntity
{
    public Guid BranchId { get; set; }
    public Guid TerminalId { get; set; }
    public Guid ShiftId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? StockReservationId { get; set; }
    public Guid? IdempotencyKey { get; set; }
    // A draft has no issued receipt number.
    public string? ReceiptNumber { get; set; }
    public string Status { get; set; } = SaleStatus.Draft;
    public string CurrencyCode { get; set; } = string.Empty;
    public bool PricesIncludeTax { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? CustomerNameSnapshot { get; set; }
    public string? CustomerPhoneSnapshot { get; set; }
    public string? DeliveryAddressSnapshot { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public CashierShift Shift { get; set; } = null!;
    public Customer? Customer { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
    public ICollection<SaleDiscount> Discounts { get; set; } = new List<SaleDiscount>();
    public ICollection<SaleReturn> Returns { get; set; } = new List<SaleReturn>();
    public ICollection<SaleStatusHistory> StatusHistory { get; set; } = new List<SaleStatusHistory>();
}
