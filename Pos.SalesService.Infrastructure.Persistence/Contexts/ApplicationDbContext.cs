using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Infrastructure.Persistence.ReadModels;

namespace Pos.SalesService.Infrastructure.Persistence.Contexts;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    // Read dynamically from the scoped user, never capture a tenant in the cached EF model.
    public Guid CurrentTenantId
    {
        get
        {
            if (_currentUser.UserType != "Tenant" ||
                !_currentUser.TenantId.HasValue || _currentUser.TenantId.Value == Guid.Empty ||
                !Guid.TryParse(_currentUser.UserId, out var userId) || userId == Guid.Empty)
                throw new UnauthorizedAccessException("A valid authenticated tenant user is required.");
            return _currentUser.TenantId.Value;
        }
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
    public DbSet<ReceiptSequence> ReceiptSequences => Set<ReceiptSequence>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<SaleDiscount> SaleDiscounts => Set<SaleDiscount>();
    public DbSet<SaleReturn> Returns => Set<SaleReturn>();
    public DbSet<SaleReturnItem> ReturnItems => Set<SaleReturnItem>();
    public DbSet<RefundPayment> RefundPayments => Set<RefundPayment>();
    public DbSet<SaleStatusHistory> SaleStatusHistory => Set<SaleStatusHistory>();
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ConfigureTenant<Customer>(modelBuilder);
        ConfigureTenant<CashierShift>(modelBuilder);
        ConfigureTenant<ReceiptSequence>(modelBuilder);
        ConfigureTenant<PaymentMethod>(modelBuilder);
        ConfigureTenant<Sale>(modelBuilder);
        ConfigureTenant<SaleItem>(modelBuilder);
        ConfigureTenant<SalePayment>(modelBuilder);
        ConfigureTenant<SaleDiscount>(modelBuilder);
        ConfigureTenant<SaleReturn>(modelBuilder);
        ConfigureTenant<SaleReturnItem>(modelBuilder);
        ConfigureTenant<RefundPayment>(modelBuilder);
        ConfigureTenant<SaleStatusHistory>(modelBuilder);

        modelBuilder.Entity<SalesBranchReadModel>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<SalesTerminalReadModel>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<SalesProductReadModel>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<SalesVariantReadModel>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<SalesTaxRateReadModel>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<SalesUnitReadModel>().HasQueryFilter(
            x => x.TenantId == CurrentTenantId || x.TenantId == null);

    }

    private void ConfigureTenant<TEntity>(ModelBuilder modelBuilder) where TEntity : SalesEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        // Protect detached updates/deletes as well: SQL must match both Id and TenantId.
        modelBuilder.Entity<TEntity>().Property(x => x.TenantId).IsConcurrencyToken();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateTenantWrites()
    {
        var tenantId = CurrentTenantId;
        foreach (var entry in ChangeTracker.Entries<SalesEntity>()
                     .Where(x => x.State == EntityState.Added ||
                                 x.State == EntityState.Modified || x.State == EntityState.Deleted))
        {
            if (entry.Entity.TenantId != tenantId ||
                (entry.State != EntityState.Added &&
                 entry.Property(x => x.TenantId).OriginalValue != tenantId))
                throw new UnauthorizedAccessException("Cannot modify another tenant's records.");
        }
    }
}
