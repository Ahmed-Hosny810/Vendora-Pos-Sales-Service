using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems", "sales", table =>
        {
            table.HasCheckConstraint("CK_SaleItems_Quantities", "[Quantity] > 0 AND [ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
            table.HasCheckConstraint("CK_SaleItems_ItemNumber", "[ItemNumber] > 0");
            table.HasCheckConstraint("CK_SaleItems_Amounts", "[UnitPrice] >= 0 AND [UnitCost] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [LineTotal] >= 0 AND [TaxRate] >= 0 AND [TaxRate] <= 100");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.VariantNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.SkuSnapshot).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.BarcodeSnapshot).HasMaxLength(100);
        builder.Property(x => x.UnitNameSnapshot).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.UnitCost).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.ReturnedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.TenantId, x.SaleId, x.ItemNumber }).IsUnique();

        builder.HasOne(x => x.Sale).WithMany(x => x.Items)
            .HasForeignKey(x => new { x.TenantId, x.SaleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
