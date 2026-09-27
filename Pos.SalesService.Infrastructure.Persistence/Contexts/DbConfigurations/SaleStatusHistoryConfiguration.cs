using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SaleStatusHistoryConfiguration : IEntityTypeConfiguration<SaleStatusHistory>
{
    public void Configure(EntityTypeBuilder<SaleStatusHistory> builder)
    {
        builder.ToTable("SaleStatusHistory", "sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OldStatus).HasMaxLength(30);
        builder.Property(x => x.NewStatus).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(300);

        builder.HasIndex(x => new { x.TenantId, x.SaleId, x.ChangedAt });

        builder.HasOne(x => x.Sale).WithMany(x => x.StatusHistory)
            .HasForeignKey(x => new { x.TenantId, x.SaleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
