using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class CashierShiftConfiguration : IEntityTypeConfiguration<CashierShift>
{
    public void Configure(EntityTypeBuilder<CashierShift> builder)
    {
        builder.ToTable("CashierShifts", "sales", table =>
        {
            table.HasCheckConstraint("CK_CashierShifts_Cash", "[OpeningCash] >= 0 AND ([ClosingCash] IS NULL OR [ClosingCash] >= 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.OpeningCash).HasPrecision(18, 2);
        builder.Property(x => x.ClosingCash).HasPrecision(18, 2);
        builder.Property(x => x.ExpectedCash).HasPrecision(18, 2);
        builder.Property(x => x.Difference).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.TenantId, x.BranchId, x.OpenedAt });
        builder.HasIndex(x => new { x.TenantId, x.TerminalId }).IsUnique().HasFilter("[Status] = N'Open'");
        builder.HasIndex(x => new { x.TenantId, x.CashierUserId }).IsUnique().HasFilter("[Status] = N'Open'");
    }
}
