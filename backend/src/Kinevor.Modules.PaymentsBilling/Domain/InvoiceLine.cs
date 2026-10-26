using Kinevor.Platform.Domain.Common;
using Kinevor.Modules.PaymentsBilling.Domain.ValueObjects;

namespace Kinevor.Modules.PaymentsBilling.Domain;

/// <summary>Input specification for an invoice line.</summary>
public sealed record InvoiceLineSpec(string Description, int Quantity, decimal UnitPrice, decimal TaxRate);

/// <summary>Invoice line item with derived tax and total amounts.</summary>
public sealed class InvoiceLine : Entity
{
    private InvoiceLine()
    {
    }

    /// <summary>Owning invoice identifier.</summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>Line description shown on the invoice.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Number of units billed.</summary>
    public int Quantity { get; private set; }

    /// <summary>Price per unit before tax.</summary>
    public Money UnitPrice { get; private set; } = null!;

    /// <summary>Tax rate applied to this line, expressed as a fraction.</summary>
    public decimal TaxRate { get; private set; }

    /// <summary>Currency inherited from the unit price.</summary>
    public string Currency => UnitPrice.Currency;

    /// <summary>Line subtotal before tax.</summary>
    public Money Subtotal => UnitPrice.Multiply(Quantity);

    /// <summary>Derived tax amount.</summary>
    public Money TaxAmount => Money.Of(Subtotal.Amount * TaxRate, Currency);

    /// <summary>Derived line total including tax.</summary>
    public Money Total => Money.Of(Subtotal.Amount + TaxAmount.Amount, Currency);

    internal static Result<InvoiceLine> Create(
        Guid invoiceId,
        string description,
        int quantity,
        decimal unitPrice,
        decimal taxRate,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<InvoiceLine>(Error.Validation("Description", "Line description is required."));
        if (description.Trim().Length > 200)
            return Result.Failure<InvoiceLine>(Error.Validation("Description", "Line description cannot exceed 200 characters."));
        if (quantity is < 1 or > 1000)
            return Result.Failure<InvoiceLine>(Error.Validation("Quantity", "Quantity must be between 1 and 1000."));
        if (unitPrice is < 0 or > 1_000_000)
            return Result.Failure<InvoiceLine>(Error.Validation("UnitPrice", "Unit price must be between 0 and 1,000,000."));
        if (taxRate is < 0 or > 1)
            return Result.Failure<InvoiceLine>(Error.Validation("TaxRate", "Tax rate must be between 0 and 1."));

        var money = Money.Create(unitPrice, currency);
        if (money.IsFailure)
            return Result.Failure<InvoiceLine>(money.Error);

        return Result.Success(new InvoiceLine
        {
            InvoiceId = invoiceId,
            Description = description.Trim(),
            Quantity = quantity,
            UnitPrice = money.Value,
            TaxRate = taxRate,
        });
    }
}
