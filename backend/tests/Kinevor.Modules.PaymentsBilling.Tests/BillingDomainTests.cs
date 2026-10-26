using Kinevor.Modules.PaymentsBilling.Domain;
using Kinevor.Modules.PaymentsBilling.Domain.ValueObjects;
using Xunit;

namespace Kinevor.Modules.PaymentsBilling.Tests;

/// <summary>Domain invariants for invoices, payments, refunds and money.</summary>
public class BillingDomainTests
{
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Invoice totals are derived from the line items, including per-line tax.</summary>
    [Fact]
    public void Invoice_DerivesTotalsFromLines()
    {
        var invoice = NewInvoice(
            new InvoiceLineSpec("Membership", 1, 49m, 0.05m),
            new InvoiceLineSpec("PT session", 2, 30m, 0.05m));

        Assert.Equal(109m, invoice.SubTotal);
        Assert.Equal(5.45m, invoice.TaxAmount);
        Assert.Equal(114.45m, invoice.Total);
        Assert.Equal(2, invoice.Lines.Count);
        Assert.Equal("INV-2026-000001", invoice.Number);
        Assert.Equal(InvoiceStatus.Open, invoice.Status);
    }

    /// <summary>Invoice creation rejects an empty basket and bad tax rates.</summary>
    [Fact]
    public void Invoice_RejectsEmptyLinesAndBadTax()
    {
        var empty = Invoice.Create(
            "INV-2026-000001", Guid.NewGuid(), "Maya Thompson", null,
            Now, Now.AddDays(14), "USD", []);

        Assert.True(empty.IsFailure);
        Assert.Equal("Validation.Lines", empty.Error.Code);

        var badTax = Invoice.Create(
            "INV-2026-000002", Guid.NewGuid(), "Maya Thompson", null,
            Now, Now.AddDays(14), "USD", [new InvoiceLineSpec("Membership", 1, 49m, 1.5m)]);

        Assert.True(badTax.IsFailure);
        Assert.Equal("Validation.TaxRate", badTax.Error.Code);
    }

    /// <summary>Partial payments keep the invoice open and full settlement closes it.</summary>
    [Fact]
    public void Invoice_AppliesPaymentsUntilSettled()
    {
        var invoice = NewInvoice(new InvoiceLineSpec("Membership", 1, 100m, 0m));

        var partial = invoice.ApplyPayment(40m, Now);
        Assert.True(partial.IsSuccess);
        Assert.Equal(60m, invoice.OutstandingAmount);
        Assert.Equal(InvoiceStatus.Open, invoice.Status);

        var full = invoice.ApplyPayment(60m, Now);
        Assert.True(full.IsSuccess);
        Assert.Equal(0m, invoice.OutstandingAmount);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(Now, invoice.PaidAt);
    }

    /// <summary>Overpayments and double settlement are rejected.</summary>
    [Fact]
    public void Invoice_RejectsOverpayment()
    {
        var invoice = NewInvoice(new InvoiceLineSpec("Membership", 1, 100m, 0m));

        var over = invoice.ApplyPayment(101m, Now);
        Assert.True(over.IsFailure);
        Assert.Equal("Invoice.Overpayment", over.Error.Code);

        Assert.True(invoice.MarkPaid(Now).IsSuccess);
        var again = invoice.ApplyPayment(1m, Now);
        Assert.True(again.IsFailure);
    }

    /// <summary>A paid invoice cannot be voided, and a void invoice cannot be paid.</summary>
    [Fact]
    public void Invoice_GuardsVoidAndPaidTransitions()
    {
        var paid = NewInvoice(new InvoiceLineSpec("Membership", 1, 100m, 0m));
        Assert.True(paid.MarkPaid(Now).IsSuccess);
        Assert.True(paid.Void(Now, "Duplicate").IsFailure);

        var open = NewInvoice(new InvoiceLineSpec("Membership", 1, 100m, 0m));
        Assert.True(open.Void(Now, "Duplicate registration").IsSuccess);
        Assert.Equal(InvoiceStatus.Void, open.Status);
        Assert.True(open.MarkPaid(Now).IsFailure);
        Assert.Equal(0m, open.OutstandingAmount);
    }

    /// <summary>Overdue status requires passing the due date and an open invoice.</summary>
    [Fact]
    public void Invoice_MarksOverdueOnlyAfterDueDate()
    {
        var invoice = NewInvoice([new InvoiceLineSpec("Membership", 1, 100m, 0m)], dueAt: Now.AddDays(5));

        Assert.True(invoice.MarkOverdue(Now).IsFailure);
        Assert.Equal(InvoiceStatus.Overdue, invoice.EffectiveStatus(Now.AddDays(6)));

        var due = NewInvoice(
            [new InvoiceLineSpec("Membership", 1, 100m, 0m)],
            dueAt: Now.AddDays(-1),
            issuedAt: Now.AddDays(-15));
        Assert.True(due.MarkOverdue(Now).IsSuccess);
        Assert.Equal(InvoiceStatus.Overdue, due.Status);
    }

    /// <summary>Refunds may be partial and cannot exceed the captured amount.</summary>
    [Fact]
    public void Payment_RefundsPartiallyThenFully()
    {
        var payment = NewPayment(100m);

        var partial = payment.Refund(Money.Of(25m), "Goodwill", Now);
        Assert.True(partial.IsSuccess);
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(75m, payment.RefundableAmount.Amount);

        var over = payment.Refund(Money.Of(80m), "Too much", Now);
        Assert.True(over.IsFailure);
        Assert.Equal("Payment.RefundExceedsBalance", over.Error.Code);

        var rest = payment.Refund(Money.Of(75m), "Full refund", Now);
        Assert.True(rest.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(0m, payment.RefundableAmount.Amount);
        Assert.Equal(2, payment.Refunds.Count);
    }

    /// <summary>Refunds require a positive amount, matching currency and a reason.</summary>
    [Fact]
    public void Payment_RefundValidatesInput()
    {
        var payment = NewPayment(100m);

        Assert.True(payment.Refund(Money.Of(0m), "None", Now).IsFailure);
        Assert.True(payment.Refund(Money.Of(10m, "EUR"), "Wrong currency", Now).IsFailure);
        Assert.True(payment.Refund(Money.Of(10m), " ", Now).IsFailure);
    }

    /// <summary>Money rounds to minor units and rejects cross-currency arithmetic.</summary>
    [Fact]
    public void Money_RoundsAndGuardsCurrency()
    {
        var usd = Money.Of(10.005m, "USD");
        Assert.Equal(10.01m, usd.Amount);
        Assert.Equal(1001, usd.MinorUnits);

        var eur = Money.Of(1m, "EUR");
        Assert.True(usd.Add(eur).IsFailure);
        Assert.True(Money.Create(-1m).IsFailure);
        Assert.True(Money.Create(1m, "ZZZ").IsFailure);
    }

    private static Invoice NewInvoice(params InvoiceLineSpec[] lines) =>
        NewInvoice(lines, Now.AddDays(14));

    private static Invoice NewInvoice(InvoiceLineSpec[] lines, DateTime dueAt, DateTime? issuedAt = null)
    {
        var result = Invoice.Create(
            "INV-2026-000001",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Maya Thompson",
            "maya.thompson@kinevor.demo",
            issuedAt ?? Now,
            dueAt,
            "USD",
            lines);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    private static Payment NewPayment(decimal amount)
    {
        var result = Payment.Record(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Maya Thompson",
            null,
            null,
            "Membership",
            amount,
            "USD",
            PaymentMethod.Card,
            "TXN-TEST-001",
            Now);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }
}
