using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.ToTable("Returns", "sales", table =>
        {
            table.HasCheckConstraint("CK_Returns_RefundAmount", "[RefundAmount] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ReturnNumber).HasMaxLength(80);
        builder.Property(x => x.Reason).HasMaxLength(300).IsRequired();
        builder.Property(x => x.RefundAmount).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.ReturnNumber }).IsUnique().HasFilter("[ReturnNumber] IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.BranchId, x.CompletedAt });

        builder.HasOne(x => x.OriginalSale).WithMany(x => x.Returns)
            .HasForeignKey(x => new { x.TenantId, x.OriginalSaleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
