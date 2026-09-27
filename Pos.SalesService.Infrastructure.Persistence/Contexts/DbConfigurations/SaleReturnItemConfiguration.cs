using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SaleReturnItemConfiguration : IEntityTypeConfiguration<SaleReturnItem>
{
    public void Configure(EntityTypeBuilder<SaleReturnItem> builder)
    {
        builder.ToTable("ReturnItems", "sales", table =>
        {
            table.HasCheckConstraint("CK_ReturnItems_Quantity", "[Quantity] > 0 AND [RefundAmount] >= 0");
            table.HasCheckConstraint("CK_ReturnItems_Restock", "[Restock] = 0 OR [StockCondition] = N'Sellable'");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.StockCondition).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RefundAmount).HasPrecision(18, 2);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.RowVersion).IsRowVersion();


        builder.HasOne(x => x.Return).WithMany(x => x.Items)
            .HasForeignKey(x => new { x.TenantId, x.ReturnId, x.OriginalSaleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id, x.OriginalSaleId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OriginalSaleItem).WithMany(x => x.ReturnItems)
            .HasForeignKey(x => new { x.TenantId, x.OriginalSaleId, x.OriginalSaleItemId })
            .HasPrincipalKey(x => new { x.TenantId, x.SaleId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
