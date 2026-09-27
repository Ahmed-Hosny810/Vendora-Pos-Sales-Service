using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class SalePaymentConfiguration : IEntityTypeConfiguration<SalePayment>
{
    public void Configure(EntityTypeBuilder<SalePayment> builder)
    {
        builder.ToTable("SalePayments", "sales", table =>
        {
            table.HasCheckConstraint("CK_SalePayments_Amounts", "[Amount] > 0 AND [ChangeAmount] >= 0 AND [ChangeAmount] <= [Amount] AND ([IsCashSnapshot] = 1 OR [ChangeAmount] = 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PaymentMethodNameSnapshot).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PaymentMethodCodeSnapshot).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.ChangeAmount).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.SaleId, x.Status });

        builder.HasOne(x => x.Sale).WithMany(x => x.Payments)
            .HasForeignKey(x => new { x.TenantId, x.SaleId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaymentMethod).WithMany(x => x.SalePayments)
            .HasForeignKey(x => new { x.TenantId, x.PaymentMethodId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
