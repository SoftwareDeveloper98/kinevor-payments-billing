using Kinevor.Modules.PaymentsBilling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kinevor.Modules.PaymentsBilling.EntityFrameworkCore;

/// <summary>EF Core mapping for the <see cref="Invoice"/> aggregate.</summary>
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    /// <summary>Configures keys, indexes and owned line items.</summary>
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Number).HasMaxLength(40).IsRequired();
        builder.HasIndex(invoice => invoice.Number).IsUnique();
        builder.Property(invoice => invoice.MemberId).IsRequired();
        builder.Property(invoice => invoice.MemberName).HasMaxLength(200).IsRequired();
        builder.Property(invoice => invoice.MemberEmail).HasMaxLength(320);
        builder.Property(invoice => invoice.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(invoice => invoice.SubTotal).HasPrecision(19, 4);
        builder.Property(invoice => invoice.TaxAmount).HasPrecision(19, 4);
        builder.Property(invoice => invoice.Total).HasPrecision(19, 4);
        builder.Property(invoice => invoice.AmountPaid).HasPrecision(19, 4);
        builder.Property(invoice => invoice.VoidReason).HasMaxLength(500);
        builder.Property(invoice => invoice.Notes).HasMaxLength(2000);
        builder.Property(invoice => invoice.RowVersion).IsConcurrencyToken();
        builder.HasIndex(invoice => new { invoice.MemberId, invoice.IssuedAt });
        builder.HasIndex(invoice => invoice.DueAt);
        builder.HasIndex(invoice => invoice.Status);

        builder.HasMany(invoice => invoice.Lines)
            .WithOne()
            .HasForeignKey(line => line.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(invoice => invoice.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(invoice => invoice.OutstandingAmount);
        builder.Ignore(invoice => invoice.IsPayable);
        builder.Ignore(invoice => invoice.DomainEvents);
    }
}

/// <summary>EF Core mapping for <see cref="InvoiceLine"/>.</summary>
public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    /// <summary>Configures columns and the owned unit price.</summary>
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("InvoiceLines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Description).HasMaxLength(200).IsRequired();
        builder.Property(line => line.TaxRate).HasPrecision(5, 4);
        builder.OwnsOne(line => line.UnitPrice, money =>
        {
            money.Property(value => value.Amount).HasColumnName("UnitPrice").HasPrecision(19, 4);
            money.Property(value => value.Currency).HasColumnName("Currency").HasMaxLength(3).IsFixedLength().IsRequired();
        });
        builder.Ignore(line => line.Currency);
        builder.Ignore(line => line.Subtotal);
        builder.Ignore(line => line.TaxAmount);
        builder.Ignore(line => line.Total);
    }
}

/// <summary>EF Core mapping for the <see cref="Payment"/> aggregate.</summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    /// <summary>Configures keys, indexes, owned amounts and refunds.</summary>
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Payments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.MemberId).IsRequired();
        builder.Property(payment => payment.MemberName).HasMaxLength(200).IsRequired();
        builder.Property(payment => payment.Description).HasMaxLength(200).IsRequired();
        builder.Property(payment => payment.Reference).HasMaxLength(80).IsRequired();
        builder.HasIndex(payment => payment.Reference).IsUnique();
        builder.Property(payment => payment.Notes).HasMaxLength(2000);
        builder.Property(payment => payment.RowVersion).IsConcurrencyToken();
        builder.HasIndex(payment => new { payment.MemberId, payment.PaidAt });
        builder.HasIndex(payment => payment.Status);
        builder.HasIndex(payment => payment.InvoiceId);

        builder.OwnsOne(payment => payment.Amount, money =>
        {
            money.Property(value => value.Amount).HasColumnName("Amount").HasPrecision(19, 4);
            money.Property(value => value.Currency).HasColumnName("Currency").HasMaxLength(3).IsFixedLength().IsRequired();
        });
        builder.OwnsOne(payment => payment.RefundedAmount, money =>
        {
            money.Property(value => value.Amount).HasColumnName("RefundedAmount").HasPrecision(19, 4);
            money.Property(value => value.Currency).HasColumnName("RefundedCurrency").HasMaxLength(3).IsFixedLength().IsRequired();
        });

        builder.HasMany(payment => payment.Refunds)
            .WithOne()
            .HasForeignKey(refund => refund.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(payment => payment.Refunds)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(payment => payment.RefundableAmount);
        builder.Ignore(payment => payment.NetAmount);
        builder.Ignore(payment => payment.DomainEvents);
    }
}

/// <summary>EF Core mapping for the <see cref="PaymentRefund"/> entity.</summary>
public sealed class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{
    /// <summary>Configures the refund table and owned amount.</summary>
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PaymentRefunds");
        builder.HasKey(refund => refund.Id);
        builder.Property(refund => refund.Id).ValueGeneratedNever();
        builder.Property(refund => refund.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(refund => refund.RefundedAt);
        builder.OwnsOne(refund => refund.Amount, money =>
        {
            money.Property(value => value.Amount).HasColumnName("Amount").HasPrecision(19, 4);
            money.Property(value => value.Currency).HasColumnName("Currency").HasMaxLength(3).IsFixedLength().IsRequired();
        });
    }
}
