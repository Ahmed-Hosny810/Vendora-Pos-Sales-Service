using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages", "sales");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Payload).IsRequired();
            builder.HasIndex(x => new { x.OccurredAt, x.Id })
                .HasFilter("[PublishedAt] IS NULL");
        }
    }
}
