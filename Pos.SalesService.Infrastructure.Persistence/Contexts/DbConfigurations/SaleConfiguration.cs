using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales", "sales", table =>
        {
            table.HasCheckConstraint("CK_Sales_Amounts", "[Subtotal] >= 0 AND [DiscountTotal] >= 0 AND [TaxTotal] >= 0 AND [Total] >= 0 AND [PaidAmount] >= 0 AND [ChangeAmount] >= 0 AND [ChangeAmount] <= [PaidAmount]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ReceiptNumber).HasMaxLength(80);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.CustomerPhoneSnapshot).HasMaxLength(30);
        builder.Property(x => x.DeliveryAddressSnapshot).HasMaxLength(500);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.ChangeAmount).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsRequired();

        //Index

        builder.HasIndex(x => new { x.TenantId, x.ReceiptNumber }).IsUnique().HasFilter("[ReceiptNumber] IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.BranchId, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.StockReservationId }).IsUnique().HasFilter("[StockReservationId] IS NOT NULL");

        builder.HasOne(x => x.Shift).WithMany(x => x.Sales)
            .HasForeignKey(x => new { x.TenantId, x.ShiftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer).WithMany(x => x.Sales)
            .HasForeignKey(x => new { x.TenantId, x.CustomerId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
