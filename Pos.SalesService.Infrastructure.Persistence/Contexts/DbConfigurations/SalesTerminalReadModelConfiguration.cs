using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Infrastructure.Persistence.ReadModels;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SalesTerminalReadModelConfiguration : IEntityTypeConfiguration<SalesTerminalReadModel>
{
    public void Configure(EntityTypeBuilder<SalesTerminalReadModel> builder)
    {
        // ToView is a read-only EF mapping to the owning service's existing table.
        // It does not create a database view or include this object in Sales migrations.
        builder.HasNoKey();
        builder.ToView("PosTerminals", "branch");
    }
}

