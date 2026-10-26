using Kinevor.Platform.Domain.Common;
using Kinevor.Modules.PaymentsBilling.Domain.ValueObjects;

namespace Kinevor.Modules.PaymentsBilling.Domain;

/// <summary>Immutable refund record belonging to a payment.</summary>
public sealed class PaymentRefund : Entity
{
    private PaymentRefund()
    {
    }

    /// <summary>Owning payment identifier.</summary>
    public Guid PaymentId { get; private set; }

    /// <summary>Refunded amount and currency.</summary>
    public Money Amount { get; private set; } = null!;

    /// <summary>Reason recorded by staff.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>UTC time the refund was issued.</summary>
    public DateTime RefundedAt { get; private set; }

    internal static PaymentRefund Create(Guid paymentId, Money amount, string reason, DateTime now) => new()
    {
        PaymentId = paymentId,
        Amount = amount,
        Reason = reason.Trim(),
        RefundedAt = now,
    };
}

/// <summary>Payment aggregate with a guarded refund workflow.</summary>
public sealed class Payment : AggregateRoot
{
    private Payment()
    {
    }

    /// <summary>External member identifier from the identity module.</summary>
    public Guid MemberId { get; private set; }

    /// <summary>Member display name at payment time.</summary>
    public string MemberName { get; private set; } = string.Empty;

    /// <summary>Linked invoice, when the payment settles one.</summary>
    public Guid? InvoiceId { get; private set; }

    /// <summary>Invoice number snapshot, when linked.</summary>
    public string? InvoiceNumber { get; private set; }

    /// <summary>Human-readable description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Captured amount and currency.</summary>
    public Money Amount { get; private set; } = null!;

    /// <summary>Total amount already refunded.</summary>
    public Money RefundedAmount { get; private set; } = null!;

    /// <summary>Payment method used.</summary>
    public PaymentMethod Method { get; private set; }

    /// <summary>Current lifecycle status.</summary>
    public PaymentStatus Status { get; private set; } = PaymentStatus.Paid;

    /// <summary>Simulated gateway transaction reference, unique per payment.</summary>
    public string Reference { get; private set; } = string.Empty;

    /// <summary>Optional staff notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>UTC capture time.</summary>
    public DateTime PaidAt { get; private set; }

    /// <summary>UTC time of the most recent refund.</summary>
    public DateTime? RefundedAt { get; private set; }

    /// <summary>Refund records.</summary>
    public List<PaymentRefund> Refunds { get; private set; } = [];

    /// <summary>Optimistic-concurrency token.</summary>
    public Guid RowVersion { get; private set; } = Guid.NewGuid();

    /// <summary>Amount still available to refund.</summary>
    public Money RefundableAmount => Amount.Subtract(RefundedAmount).Value;

    /// <summary>Net captured amount after refunds.</summary>
    public Money NetAmount => RefundableAmount;

    /// <summary>Records a simulated captured payment.</summary>
    public static Result<Payment> Record(
        Guid memberId,
        string memberName,
        Guid? invoiceId,
        string? invoiceNumber,
        string description,
        decimal amount,
        string currency,
        PaymentMethod method,
        string reference,
        DateTime now,
        string? notes = null)
    {
        if (memberId == Guid.Empty)
            return Result.Failure<Payment>(Error.Validation("MemberId", "Member id is required."));
        if (string.IsNullOrWhiteSpace(memberName))
            return Result.Failure<Payment>(Error.Validation("MemberName", "Member name is required."));
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<Payment>(Error.Validation("Description", "Payment description is required."));
        if (description.Trim().Length > 200)
            return Result.Failure<Payment>(Error.Validation("Description", "Payment description cannot exceed 200 characters."));
        if (string.IsNullOrWhiteSpace(reference))
            return Result.Failure<Payment>(Error.Validation("Reference", "Payment reference is required."));
        if (reference.Trim().Length > 80)
            return Result.Failure<Payment>(Error.Validation("Reference", "Payment reference cannot exceed 80 characters."));

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsFailure)
            return Result.Failure<Payment>(moneyResult.Error);
        if (!moneyResult.Value.IsPositive)
            return Result.Failure<Payment>(Error.Validation("Amount", "Payment amount must be positive."));

        return Result.Success(new Payment
        {
            MemberId = memberId,
            MemberName = memberName.Trim(),
            InvoiceId = invoiceId,
            InvoiceNumber = string.IsNullOrWhiteSpace(invoiceNumber) ? null : invoiceNumber.Trim(),
            Description = description.Trim(),
            Amount = moneyResult.Value,
            RefundedAmount = Money.Zero(moneyResult.Value.Currency),
            Method = method,
            Status = PaymentStatus.Paid,
            Reference = reference.Trim().ToUpperInvariant(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PaidAt = now,
        });
    }

    /// <summary>Issues a full or partial refund and updates the status.</summary>
    public Result Refund(Money amount, string reason, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (Status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
            return Result.Failure(Error.Business("Payment.NotRefundable", $"A {Status} payment cannot be refunded."));
        if (!amount.IsPositive)
            return Result.Failure(Error.Validation("Amount", "Refund amount must be positive."));
        if (amount.Currency != Amount.Currency)
            return Result.Failure(Error.Business("Payment.CurrencyMismatch", "Refund currency must match the payment currency."));
        if (amount.IsGreaterThan(RefundableAmount))
            return Result.Failure(Error.Business("Payment.RefundExceedsBalance",
                $"Refund exceeds the remaining refundable amount of {Amount.Currency} {RefundableAmount.Amount:F2}."));
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("Reason", "A refund reason is required."));

        Refunds.Add(PaymentRefund.Create(Id, amount, reason, now));
        RefundedAmount = Money.Of(RefundedAmount.Amount + amount.Amount, Amount.Currency);
        RefundedAt = now;
        Status = RefundedAmount.Amount >= Amount.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        RowVersion = Guid.NewGuid();
        return Result.Success();
    }
}
