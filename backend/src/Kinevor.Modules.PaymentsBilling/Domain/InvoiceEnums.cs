namespace Kinevor.Modules.PaymentsBilling.Domain;

/// <summary>Lifecycle state of an invoice.</summary>
public enum InvoiceStatus
{
    /// <summary>Issued and awaiting payment.</summary>
    Open,

    /// <summary>Fully settled.</summary>
    Paid,

    /// <summary>Cancelled before settlement.</summary>
    Void,

    /// <summary>Past its due date without settlement.</summary>
    Overdue,
}

/// <summary>Supported payment methods in the simulated gateway.</summary>
public enum PaymentMethod
{
    /// <summary>Simulated card transaction.</summary>
    Card,

    /// <summary>Cash received at the front desk.</summary>
    Cash,

    /// <summary>Bank transfer reference recorded by staff.</summary>
    BankTransfer,
}

/// <summary>Lifecycle state of a payment.</summary>
public enum PaymentStatus
{
    /// <summary>Awaiting gateway confirmation.</summary>
    Pending,

    /// <summary>Captured successfully.</summary>
    Paid,

    /// <summary>Gateway declined the payment.</summary>
    Failed,

    /// <summary>One or more partial refunds have been issued.</summary>
    PartiallyRefunded,

    /// <summary>Fully refunded.</summary>
    Refunded,
}
