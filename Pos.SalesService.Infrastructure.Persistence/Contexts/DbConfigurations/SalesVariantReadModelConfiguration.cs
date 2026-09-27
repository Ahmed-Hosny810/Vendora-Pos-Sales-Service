using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Infrastructure.Persistence.ReadModels;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SalesVariantReadModelConfiguration : IEntityTypeConfiguration<SalesVariantReadModel>
{
    public void Configure(EntityTypeBuilder<SalesVariantReadModel> builder)
    {
        // ToView is a read-only EF mapping to the owning service's existing table.
        // It does not create a database view or include this object in Sales migrations.
        builder.HasNoKey();
        builder.ToView("ProductVariants", "catalog");
        builder.Property(x => x.SellingPrice).HasPrecision(18, 2);
        builder.Property(x => x.CostPrice).HasPrecision(18, 2);
    }
}
