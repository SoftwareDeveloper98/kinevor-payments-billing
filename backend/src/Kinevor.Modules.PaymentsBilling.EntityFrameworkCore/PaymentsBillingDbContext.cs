using Kinevor.Modules.PaymentsBilling.Application;
using Kinevor.Modules.PaymentsBilling.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kinevor.Modules.PaymentsBilling.EntityFrameworkCore;

/// <summary>EF Core context owned by the payments and billing module.</summary>
/// <param name="options">Provider and connection options supplied by the host.</param>
public sealed class PaymentsBillingDbContext(DbContextOptions<PaymentsBillingDbContext> options)
    : DbContext(options), IPaymentsBillingUnitOfWork
{
    /// <summary>Invoice aggregates.</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>Payment aggregates.</summary>
    public DbSet<Payment> Payments => Set<Payment>();

    /// <summary>Refund records owned by payments.</summary>
    public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();

    /// <summary>Applies module configurations.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceLineConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentRefundConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Stamps audit fields and persists pending changes.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Kinevor.Platform.Domain.Common.Entity>())
        {
            if (entry.State == EntityState.Added)
                entry.Property(nameof(Kinevor.Platform.Domain.Common.Entity.CreatedAt)).CurrentValue = now;

            if (entry.State == EntityState.Modified)
                entry.Property(nameof(Kinevor.Platform.Domain.Common.Entity.UpdatedAt)).CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
