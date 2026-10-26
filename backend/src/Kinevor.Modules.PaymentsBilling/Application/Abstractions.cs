using Kinevor.Modules.PaymentsBilling.Domain;

namespace Kinevor.Modules.PaymentsBilling.Application;

/// <summary>Snapshot of invoice balances used by billing statistics.</summary>
public sealed record InvoiceStatsSnapshot(
    int OpenCount,
    int OverdueCount,
    int PaidCount,
    int TotalCount,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    DateTime? NextDueAt);

/// <summary>Persistence contract for invoices.</summary>
public interface IInvoiceRepository
{
    /// <summary>Loads an invoice with its line items.</summary>
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Finds an invoice by its unique number.</summary>
    Task<Invoice?> GetByNumberAsync(string number, CancellationToken ct = default);

    /// <summary>Pages invoices by search text, effective status and optional member.</summary>
    Task<(IReadOnlyList<Invoice> Items, int Total)> SearchAsync(
        string? search,
        InvoiceStatus? status,
        Guid? memberId,
        DateTime now,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Returns the next number sequence for a year.</summary>
    Task<int> GetNextSequenceAsync(int year, CancellationToken ct = default);

    /// <summary>Counts and totals open, overdue and paid invoices.</summary>
    Task<InvoiceStatsSnapshot> GetStatsAsync(
        Guid? memberId, DateTime now, CancellationToken ct = default);

    /// <summary>Tracks a new invoice.</summary>
    void Add(Invoice invoice);
}

/// <summary>Persistence contract for payments and refunds.</summary>
public interface IPaymentRepository
{
    /// <summary>Loads a payment with its refund history.</summary>
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Finds a payment by simulated gateway reference.</summary>
    Task<Payment?> GetByReferenceAsync(string reference, CancellationToken ct = default);

    /// <summary>Pages payments by search text, status, method and optional member.</summary>
    Task<(IReadOnlyList<Payment> Items, int Total)> SearchAsync(
        string? search,
        PaymentStatus? status,
        PaymentMethod? method,
        Guid? memberId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Returns payments captured inside a UTC date range.</summary>
    Task<IReadOnlyList<Payment>> GetCapturedBetweenAsync(
        DateTime from, DateTime to, Guid? memberId, CancellationToken ct = default);

    /// <summary>Returns refunds issued inside a UTC date range.</summary>
    Task<IReadOnlyList<PaymentRefund>> GetRefundsBetweenAsync(
        DateTime from, DateTime to, Guid? memberId, CancellationToken ct = default);

    /// <summary>Tracks a new payment.</summary>
    void Add(Payment payment);
}

/// <summary>Unit of work owned by the payments and billing module.</summary>
public interface IPaymentsBillingUnitOfWork
{
    /// <summary>Persists pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
