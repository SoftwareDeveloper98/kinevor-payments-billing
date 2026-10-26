using System.Globalization;
using Kinevor.Platform.Domain.Common;

namespace Kinevor.Modules.PaymentsBilling.Domain.ValueObjects;

/// <summary>
/// ISO 4217 money. Amounts are rounded to the currency's minor units and arithmetic only
/// operates between identical currencies.
/// </summary>
public sealed class Money : ValueObject
{
    private static readonly Dictionary<string, int> MinorUnitsTable = new(StringComparer.Ordinal)
    {
        ["AED"] = 2,
        ["AFN"] = 2,
        ["ALL"] = 2,
        ["AMD"] = 2,
        ["ANG"] = 2,
        ["AOA"] = 2,
        ["ARS"] = 2,
        ["AUD"] = 2,
        ["AWG"] = 2,
        ["AZN"] = 2,
        ["BAM"] = 2,
        ["BBD"] = 2,
        ["BDT"] = 2,
        ["BGN"] = 2,
        ["BHD"] = 3,
        ["BIF"] = 0,
        ["BMD"] = 2,
        ["BND"] = 2,
        ["BOB"] = 2,
        ["BRL"] = 2,
        ["BSD"] = 2,
        ["BTN"] = 2,
        ["BWP"] = 2,
        ["BYN"] = 2,
        ["BZD"] = 2,
        ["CAD"] = 2,
        ["CDF"] = 2,
        ["CHF"] = 2,
        ["CLP"] = 0,
        ["CNY"] = 2,
        ["COP"] = 2,
        ["CRC"] = 2,
        ["CUP"] = 2,
        ["CVE"] = 2,
        ["CZK"] = 2,
        ["DJF"] = 0,
        ["DKK"] = 2,
        ["DOP"] = 2,
        ["DZD"] = 2,
        ["EGP"] = 2,
        ["ERN"] = 2,
        ["ETB"] = 2,
        ["EUR"] = 2,
        ["FJD"] = 2,
        ["FKP"] = 2,
        ["GBP"] = 2,
        ["GEL"] = 2,
        ["GHS"] = 2,
        ["GIP"] = 2,
        ["GMD"] = 2,
        ["GNF"] = 0,
        ["GTQ"] = 2,
        ["GYD"] = 2,
        ["HKD"] = 2,
        ["HNL"] = 2,
        ["HRK"] = 2,
        ["HTG"] = 2,
        ["HUF"] = 2,
        ["IDR"] = 2,
        ["ILS"] = 2,
        ["INR"] = 2,
        ["IQD"] = 3,
        ["IRR"] = 2,
        ["ISK"] = 0,
        ["JMD"] = 2,
        ["JOD"] = 3,
        ["JPY"] = 0,
        ["KES"] = 2,
        ["KGS"] = 2,
        ["KHR"] = 2,
        ["KMF"] = 0,
        ["KRW"] = 0,
        ["KWD"] = 3,
        ["KYD"] = 2,
        ["KZT"] = 2,
        ["LAK"] = 2,
        ["LBP"] = 2,
        ["LKR"] = 2,
        ["LRD"] = 2,
        ["LSL"] = 2,
        ["LYD"] = 3,
        ["MAD"] = 2,
        ["MDL"] = 2,
        ["MGA"] = 2,
        ["MKD"] = 2,
        ["MMK"] = 2,
        ["MNT"] = 2,
        ["MOP"] = 2,
        ["MRU"] = 2,
        ["MUR"] = 2,
        ["MVR"] = 2,
        ["MWK"] = 2,
        ["MXN"] = 2,
        ["MYR"] = 2,
        ["MZN"] = 2,
        ["NAD"] = 2,
        ["NGN"] = 2,
        ["NIO"] = 2,
        ["NOK"] = 2,
        ["NPR"] = 2,
        ["NZD"] = 2,
        ["OMR"] = 3,
        ["PAB"] = 2,
        ["PEN"] = 2,
        ["PGK"] = 2,
        ["PHP"] = 2,
        ["PKR"] = 2,
        ["PLN"] = 2,
        ["PYG"] = 0,
        ["QAR"] = 2,
        ["RON"] = 2,
        ["RSD"] = 2,
        ["RUB"] = 2,
        ["RWF"] = 0,
        ["SAR"] = 2,
        ["SBD"] = 2,
        ["SCR"] = 2,
        ["SDG"] = 2,
        ["SEK"] = 2,
        ["SGD"] = 2,
        ["SHP"] = 2,
        ["SLE"] = 2,
        ["SOS"] = 2,
        ["SRD"] = 2,
        ["SSP"] = 2,
        ["STN"] = 2,
        ["SYP"] = 2,
        ["SZL"] = 2,
        ["THB"] = 2,
        ["TJS"] = 2,
        ["TMT"] = 2,
        ["TND"] = 3,
        ["TOP"] = 2,
        ["TRY"] = 2,
        ["TTD"] = 2,
        ["TWD"] = 2,
        ["TZS"] = 2,
        ["UAH"] = 2,
        ["UGX"] = 0,
        ["USD"] = 2,
        ["UYU"] = 2,
        ["UZS"] = 2,
        ["VES"] = 2,
        ["VND"] = 0,
        ["VUV"] = 0,
        ["WST"] = 2,
        ["XAF"] = 0,
        ["XCD"] = 2,
        ["XOF"] = 0,
        ["XPF"] = 0,
        ["YER"] = 2,
        ["ZAR"] = 2,
        ["ZMW"] = 2,
        ["ZWG"] = 2,
    };

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>Decimal amount, rounded to the currency's minor units.</summary>
    public decimal Amount { get; }

    /// <summary>ISO 4217 alphabetic currency code.</summary>
    public string Currency { get; }

    /// <summary>Number of decimal (minor-unit) digits for the currency.</summary>
    public int MinorUnitDigits => MinorUnitsTable[Currency];

    /// <summary>Amount expressed as an integral count of minor units.</summary>
    public long MinorUnits => (long)decimal.Round(Amount * MinorUnitFactor, 0, MidpointRounding.AwayFromZero);

    private decimal MinorUnitFactor => (decimal)Math.Pow(10, MinorUnitDigits);

    /// <summary>Returns whether the code is a supported ISO 4217 currency.</summary>
    public static bool IsKnownCurrency(string? currency) =>
        !string.IsNullOrWhiteSpace(currency) &&
        MinorUnitsTable.ContainsKey(currency.Trim().ToUpperInvariant());

    /// <summary>Minor-unit digit count for a currency.</summary>
    public static int MinorUnitsOf(string currency) =>
        MinorUnitsTable.TryGetValue(currency.Trim().ToUpperInvariant(), out var digits)
            ? digits
            : throw new ArgumentException($"Unknown ISO 4217 currency '{currency}'.", nameof(currency));

    /// <summary>Validates amount and currency and builds a rounded instance.</summary>
    public static Result<Money> Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            return Result.Failure<Money>(Error.Validation("Money", "Amount cannot be negative."));
        if (string.IsNullOrWhiteSpace(currency))
            return Result.Failure<Money>(Error.Validation("Money", "Currency is required."));

        var code = currency.Trim().ToUpperInvariant();
        if (!MinorUnitsTable.TryGetValue(code, out var digits))
            return Result.Failure<Money>(Error.Validation("Currency", $"Unknown ISO 4217 currency code '{code}'."));

        return Result.Success(Build(amount, code, digits));
    }

    /// <summary>Builds a money value, throwing for unknown currencies.</summary>
    public static Money Of(decimal amount, string currency = "USD")
    {
        var code = currency.Trim().ToUpperInvariant();
        if (!MinorUnitsTable.TryGetValue(code, out var digits))
            throw new ArgumentException($"Unknown ISO 4217 currency code '{code}'.", nameof(currency));
        return Build(amount, code, digits);
    }

    /// <summary>Returns a zero amount in the requested currency.</summary>
    public static Money Zero(string currency = "USD") => Of(0m, currency);

    /// <summary>Adds another money of the same currency.</summary>
    public Result<Money> Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Currency != Currency)
            return Result.Failure<Money>(Error.Business("Money.CurrencyMismatch",
                $"Cannot add {other.Currency} to {Currency}."));
        return Result.Success(Of(Amount + other.Amount, Currency));
    }

    /// <summary>Subtracts another money of the same currency, refusing negative results.</summary>
    public Result<Money> Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Currency != Currency)
            return Result.Failure<Money>(Error.Business("Money.CurrencyMismatch",
                $"Cannot subtract {other.Currency} from {Currency}."));
        if (other.Amount > Amount)
            return Result.Failure<Money>(Error.Business("Money.NegativeResult", "Resulting amount cannot be negative."));
        return Result.Success(Of(Amount - other.Amount, Currency));
    }

    /// <summary>Scales the amount by an integer factor.</summary>
    public Money Multiply(int factor) => Of(Amount * factor, Currency);

    /// <summary>Returns whether this amount is greater than another of the same currency.</summary>
    public bool IsGreaterThan(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);
        return Amount > other.Amount;
    }

    /// <summary>Returns whether this amount is at least another of the same currency.</summary>
    public bool IsAtLeast(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);
        return Amount >= other.Amount;
    }

    /// <summary>Returns whether this amount is positive.</summary>
    public bool IsPositive => Amount > 0m;

    /// <summary>Creates money from an integral minor-unit amount.</summary>
    public static Result<Money> FromMinorUnits(long minorUnits, string currency) =>
        Create(minorUnits / (decimal)Math.Pow(10, MinorUnitsOf(currency)), currency);

    private void EnsureSameCurrency(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException($"Cannot compare {other.Currency} with {Currency}.");
    }

    private static Money Build(decimal amount, string code, int digits) =>
        new(decimal.Round(amount, digits, MidpointRounding.AwayFromZero), code);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    /// <summary>Culture-invariant rendering with the currency's minor units.</summary>
    public override string ToString() =>
        $"{Currency} {Amount.ToString($"F{MinorUnitDigits}", CultureInfo.InvariantCulture)}";
}
