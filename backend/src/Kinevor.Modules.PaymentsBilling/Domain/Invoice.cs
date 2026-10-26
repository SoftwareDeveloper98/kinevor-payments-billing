using Kinevor.Platform.Domain.Common;
using Kinevor.Modules.PaymentsBilling.Domain.ValueObjects;

namespace Kinevor.Modules.PaymentsBilling.Domain;

/// <summary>
/// Invoice aggregate. Totals are always derived from the line items, and the number follows
/// the per-year <c>INV-{year}-{sequence}</c> convention.
/// </summary>
public sealed class Invoice : AggregateRoot
{
    private Invoice()
    {
    }

    /// <summary>Human-readable invoice number.</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>External member identifier from the identity module; never a cross-module foreign key.</summary>
    public Guid MemberId { get; private set; }

    /// <summary>Member display name at issue time.</summary>
    public string MemberName { get; private set; } = string.Empty;

    /// <summary>Member e-mail at issue time, when supplied.</summary>
    public string? MemberEmail { get; private set; }

    /// <summary>UTC issue time.</summary>
    public DateTime IssuedAt { get; private set; }

    /// <summary>UTC payment due date.</summary>
    public DateTime DueAt { get; private set; }

    /// <summary>Current lifecycle status.</summary>
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Open;

    /// <summary>ISO 4217 currency of all line amounts.</summary>
    public string Currency { get; private set; } = "USD";

    /// <summary>Sum of line subtotals before tax.</summary>
    public decimal SubTotal { get; private set; }

    /// <summary>Sum of line taxes.</summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>Subtotal plus tax.</summary>
    public decimal Total { get; private set; }

    /// <summary>Amount settled by recorded payments.</summary>
    public decimal AmountPaid { get; private set; }

    /// <summary>UTC time the invoice became fully paid.</summary>
    public DateTime? PaidAt { get; private set; }

    /// <summary>UTC time the invoice was voided.</summary>
    public DateTime? VoidedAt { get; private set; }

    /// <summary>Reason recorded when the invoice was voided.</summary>
    public string? VoidReason { get; private set; }

    /// <summary>Optional internal notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>Line items composing the invoice.</summary>
    public List<InvoiceLine> Lines { get; private set; } = [];

    /// <summary>Optimistic-concurrency token.</summary>
    public Guid RowVersion { get; private set; } = Guid.NewGuid();

    /// <summary>Remaining balance still owed.</summary>
    public decimal OutstandingAmount =>
        Status is InvoiceStatus.Paid or InvoiceStatus.Void ? 0m : Math.Max(0m, Total - AmountPaid);

    /// <summary>Whether the invoice can accept a new payment.</summary>
    public bool IsPayable => Status is (InvoiceStatus.Open or InvoiceStatus.Overdue) && OutstandingAmount > 0m;

    /// <summary>Computes the status shown to callers, promoting past-due open invoices to overdue.</summary>
    public InvoiceStatus EffectiveStatus(DateTime now) =>
        Status == InvoiceStatus.Open && now.Date > DueAt.Date ? InvoiceStatus.Overdue : Status;

    /// <summary>Formats an invoice number for a year and sequence.</summary>
    public static string FormatNumber(int year, int sequence) => $"INV-{year}-{sequence:D6}";

    /// <summary>Builds an invoice from line specifications and derives every total.</summary>
    public static Result<Invoice> Create(
        string number,
        Guid memberId,
        string memberName,
        string? memberEmail,
        DateTime issuedAt,
        DateTime dueAt,
        string currency,
        IEnumerable<InvoiceLineSpec> lines,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(number))
            return Result.Failure<Invoice>(Error.Validation("Number", "Invoice number is required."));
        if (memberId == Guid.Empty)
            return Result.Failure<Invoice>(Error.Validation("MemberId", "Member id is required."));
        if (string.IsNullOrWhiteSpace(memberName))
            return Result.Failure<Invoice>(Error.Validation("MemberName", "Member name is required."));
        if (memberName.Trim().Length > 200)
            return Result.Failure<Invoice>(Error.Validation("MemberName", "Member name cannot exceed 200 characters."));
        if (dueAt < issuedAt)
            return Result.Failure<Invoice>(Error.Validation("DueAt", "Due date cannot be before the issue date."));
        if (!Money.IsKnownCurrency(currency))
            return Result.Failure<Invoice>(Error.Validation("Currency", $"Unknown ISO 4217 currency code '{currency}'."));

        var specs = lines.ToList();
        if (specs.Count == 0)
            return Result.Failure<Invoice>(Error.Validation("Lines", "Invoice needs at least one line item."));
        if (specs.Count > 50)
            return Result.Failure<Invoice>(Error.Validation("Lines", "An invoice can contain at most 50 line items."));

        var invoice = new Invoice
        {
            Number = number.Trim().ToUpperInvariant(),
            MemberId = memberId,
            MemberName = memberName.Trim(),
            MemberEmail = string.IsNullOrWhiteSpace(memberEmail) ? null : memberEmail.Trim(),
            IssuedAt = issuedAt,
            DueAt = dueAt,
            Currency = currency.Trim().ToUpperInvariant(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };

        foreach (var spec in specs)
        {
            var lineResult = InvoiceLine.Create(
                invoice.Id, spec.Description, spec.Quantity, spec.UnitPrice, spec.TaxRate, invoice.Currency);
            if (lineResult.IsFailure)
                return Result.Failure<Invoice>(lineResult.Error);

            invoice.Lines.Add(lineResult.Value);
        }

        invoice.Recalculate();
        return Result.Success(invoice);
    }

    /// <summary>Applies a payment to the invoice and marks it paid once fully settled.</summary>
    public Result ApplyPayment(decimal amount, DateTime now)
    {
        if (Status is InvoiceStatus.Paid or InvoiceStatus.Void)
            return Result.Failure(Error.Business("Invoice.NotPayable", $"A {Status} invoice cannot accept a payment."));
        if (amount <= 0m)
            return Result.Failure(Error.Validation("Amount", "Payment amount must be positive."));
        if (amount > OutstandingAmount)
            return Result.Failure(Error.Business("Invoice.Overpayment",
                $"Payment exceeds the outstanding balance of {Currency} {OutstandingAmount:F2}."));

        AmountPaid = decimal.Round(AmountPaid + amount, 2, MidpointRounding.AwayFromZero);
        Status = AmountPaid >= Total ? InvoiceStatus.Paid : InvoiceStatus.Open;
        if (Status == InvoiceStatus.Paid)
            PaidAt = now;
        RowVersion = Guid.NewGuid();
        return Result.Success();
    }

    /// <summary>Marks the invoice fully paid, for settlements recorded outside this module.</summary>
    public Result MarkPaid(DateTime now)
    {
        if (Status == InvoiceStatus.Paid)
            return Result.Success();
        if (Status == InvoiceStatus.Void)
            return Result.Failure(Error.Business("Invoice.Voided", "A void invoice cannot be marked paid."));

        AmountPaid = Total;
        Status = InvoiceStatus.Paid;
        PaidAt = now;
        RowVersion = Guid.NewGuid();
        return Result.Success();
    }

    /// <summary>Voids an unpaid invoice with a reason.</summary>
    public Result Void(DateTime now, string reason)
    {
        if (Status == InvoiceStatus.Void)
            return Result.Success();
        if (Status == InvoiceStatus.Paid)
            return Result.Failure(Error.Business("Invoice.Paid", "A paid invoice cannot be voided."));
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("Reason", "A void reason is required."));

        Status = InvoiceStatus.Void;
        VoidedAt = now;
        VoidReason = reason.Trim()[..Math.Min(reason.Trim().Length, 500)];
        RowVersion = Guid.NewGuid();
        return Result.Success();
    }

    /// <summary>Marks an open invoice overdue once its due date has passed.</summary>
    public Result MarkOverdue(DateTime now)
    {
        if (Status == InvoiceStatus.Overdue)
            return Result.Success();
        if (Status != InvoiceStatus.Open)
            return Result.Failure(Error.Business("Invoice.NotOpen", $"A {Status} invoice cannot be marked overdue."));
        if (now.Date <= DueAt.Date)
            return Result.Failure(Error.Business("Invoice.NotDue", "The invoice due date has not passed."));

        Status = InvoiceStatus.Overdue;
        RowVersion = Guid.NewGuid();
        return Result.Success();
    }

    private void Recalculate()
    {
        SubTotal = decimal.Round(Lines.Sum(line => line.Subtotal.Amount), 2, MidpointRounding.AwayFromZero);
        TaxAmount = decimal.Round(Lines.Sum(line => line.TaxAmount.Amount), 2, MidpointRounding.AwayFromZero);
        Total = decimal.Round(SubTotal + TaxAmount, 2, MidpointRounding.AwayFromZero);
    }
}
