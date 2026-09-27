using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class ReceiptSequenceConfiguration : IEntityTypeConfiguration<ReceiptSequence>
{
    public void Configure(EntityTypeBuilder<ReceiptSequence> builder)
    {
        builder.ToTable("ReceiptSequences", "sales", table =>
        {
            table.HasCheckConstraint("CK_ReceiptSequences_LastNumber", "[LastNumber] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DocumentType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.TenantId, x.BranchId, x.DocumentType }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Prefix }).IsUnique();
    }
}
