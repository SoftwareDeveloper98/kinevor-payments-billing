using Kinevor.Modules.PaymentsBilling.Domain;
using Kinevor.Modules.PaymentsBilling.Domain.ValueObjects;
using Kinevor.Modules.PaymentsBilling.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kinevor.Modules.PaymentsBilling.DemoApi.Data;

/// <summary>Seeds a deterministic billing story for the standalone demo.</summary>
public static class DemoDataSeeder
{
    /// <summary>Creates invoices, payments and one refund when the database is empty.</summary>
    public static async Task SeedAsync(PaymentsBillingDbContext db, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (await db.Invoices.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        var year = now.Year;

        var mayaPaid = Create(
            Invoice.FormatNumber(year, 1), DemoMembers.Maya, "Maya Thompson", "maya.thompson@kinevor.demo",
            now.AddDays(-49), now.AddDays(-35),
            [
                new("Standard membership - monthly", 1, 49m, 0.05m),
                new("Personal training session", 2, 30m, 0.05m),
            ]);
        var mayaOverdue = Create(
            Invoice.FormatNumber(year, 2), DemoMembers.Maya, "Maya Thompson", "maya.thompson@kinevor.demo",
            now.AddDays(-19), now.AddDays(-5),
            [
                new("Standard membership - monthly", 1, 49m, 0.05m),
                new("Guest day passes", 2, 15m, 0.05m),
            ]);
        var mayaOpen = Create(
            Invoice.FormatNumber(year, 3), DemoMembers.Maya, "Maya Thompson", "maya.thompson@kinevor.demo",
            now.AddDays(-2), now.AddDays(12),
            [
                new("Annual locker rental", 1, 120m, 0.05m),
            ],
            "Locker 128, renews annually.");
        var mayaVoid = Create(
            Invoice.FormatNumber(year, 4), DemoMembers.Maya, "Maya Thompson", "maya.thompson@kinevor.demo",
            now.AddDays(-24), now.AddDays(-10),
            [
                new("Nutrition consultation", 1, 40m, 0.05m),
            ]);
        var danielOverdue = Create(
            Invoice.FormatNumber(year, 5), DemoMembers.Daniel, "Daniel Okafor", "daniel.okafor@kinevor.demo",
            now.AddDays(-27), now.AddDays(-13),
            [
                new("Premium membership - monthly", 1, 80m, 0.05m),
            ]);
        var danielOpen = Create(
            Invoice.FormatNumber(year, 6), DemoMembers.Daniel, "Daniel Okafor", "daniel.okafor@kinevor.demo",
            now.AddDays(-4), now.AddDays(10),
            [
                new("Premium membership - monthly", 1, 80m, 0.05m),
                new("Recovery suite add-on", 1, 25m, 0.05m),
            ]);
        var sofiaPaid = Create(
            Invoice.FormatNumber(year, 7), DemoMembers.Sofia, "Sofia Rossi", "sofia.rossi@kinevor.demo",
            now.AddDays(-35), now.AddDays(-21),
            [
                new("Pilates membership - monthly", 1, 65m, 0.05m),
            ]);

        if (mayaVoid.Void(now.AddDays(-20), "Duplicate registration").IsFailure)
            throw new InvalidOperationException("Could not seed the void invoice.");

        db.Invoices.AddRange(
            mayaPaid, mayaOverdue, mayaOpen, mayaVoid,
            danielOverdue, danielOpen, sofiaPaid);
        await db.SaveChangesAsync(ct);

        var mayaCard = RecordPayment(
            mayaPaid, "Card", "TXN-SEED-MAYA-001", mayaPaid.Total, now.AddDays(-45), "Membership and PT");
        var mayaCash = RecordPayment(
            mayaOverdue, "Cash", "TXN-SEED-MAYA-002", 30m, now.AddDays(-8), "Part payment at front desk");
        var danielTransfer = RecordPayment(
            sofiaPaid, "BankTransfer", "TXN-SEED-SOFIA-001", sofiaPaid.Total, now.AddDays(-20), "Pilates renewal");

        db.Payments.AddRange(mayaCard, mayaCash, danielTransfer);

        var refundAmount = Money.Of(10m, BillingDefaultsCurrency);
        var refund = danielTransfer.Refund(refundAmount, "Goodwill credit for a missed class", now.AddDays(-6));
        if (refund.IsFailure)
            throw new InvalidOperationException(refund.Error.Message);

        await db.SaveChangesAsync(ct);
    }

    private const string BillingDefaultsCurrency = "USD";

    private static Invoice Create(
        string number,
        Guid memberId,
        string memberName,
        string memberEmail,
        DateTime issuedAt,
        DateTime dueAt,
        IReadOnlyList<InvoiceLineSpec> lines,
        string? notes = null)
    {
        var result = Invoice.Create(
            number, memberId, memberName, memberEmail, issuedAt, dueAt,
            BillingDefaultsCurrency, lines, notes);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error.Message);
        return result.Value;
    }

    private static Payment RecordPayment(
        Invoice invoice,
        string method,
        string reference,
        decimal amount,
        DateTime paidAt,
        string description)
    {
        var applied = invoice.ApplyPayment(amount, paidAt);
        if (applied.IsFailure)
            throw new InvalidOperationException(applied.Error.Message);

        var result = Payment.Record(
            invoice.MemberId,
            invoice.MemberName,
            invoice.Id,
            invoice.Number,
            description,
            amount,
            invoice.Currency,
            Enum.Parse<PaymentMethod>(method),
            reference,
            paidAt);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error.Message);
        return result.Value;
    }
}
