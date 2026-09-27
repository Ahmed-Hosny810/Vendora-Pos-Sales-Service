using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts.DbConfigurations;

public class RefundPaymentConfiguration : IEntityTypeConfiguration<RefundPayment>
{
    public void Configure(EntityTypeBuilder<RefundPayment> builder)
    {
        builder.ToTable("RefundPayments", "sales", table =>
        {
            table.HasCheckConstraint("CK_RefundPayments_Amount", "[Amount] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PaymentMethodNameSnapshot).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PaymentMethodCodeSnapshot).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.ReturnId, x.Status });

        builder.HasOne(x => x.Return).WithMany(x => x.RefundPayments)
            .HasForeignKey(x => new { x.TenantId, x.ReturnId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Shift).WithMany(x => x.RefundPayments)
            .HasForeignKey(x => new { x.TenantId, x.ShiftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaymentMethod).WithMany(x => x.RefundPayments)
            .HasForeignKey(x => new { x.TenantId, x.PaymentMethodId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
