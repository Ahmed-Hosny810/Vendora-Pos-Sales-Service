using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SaleDiscountConfiguration : IEntityTypeConfiguration<SaleDiscount>
{
    public void Configure(EntityTypeBuilder<SaleDiscount> builder)
    {
        builder.ToTable("SaleDiscounts", "sales", table =>
        {
            table.HasCheckConstraint("CK_SaleDiscounts_Value", "[Value] > 0 AND [Amount] >= 0 AND ([DiscountType] <> N'Percentage' OR [Value] <= 100)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DiscountType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Value).HasPrecision(18, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);


        builder.HasOne(x => x.Sale).WithMany(x => x.Discounts)
            .HasForeignKey(x => new { x.TenantId, x.SaleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SaleItem).WithMany(x => x.Discounts)
            .HasForeignKey(x => new { x.TenantId, x.SaleId, x.SaleItemId })
            .HasPrincipalKey(x => new { x.TenantId, x.SaleId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
