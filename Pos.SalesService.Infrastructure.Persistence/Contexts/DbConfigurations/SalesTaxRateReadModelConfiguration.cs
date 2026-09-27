using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Infrastructure.Persistence.ReadModels;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SalesTaxRateReadModelConfiguration : IEntityTypeConfiguration<SalesTaxRateReadModel>
{
    public void Configure(EntityTypeBuilder<SalesTaxRateReadModel> builder)
    {
        builder.HasNoKey();
        builder.ToView("TaxRates", "catalog");
        builder.Property(x => x.Rate).HasPrecision(5, 2);
    }
}
