using Kinevor.Modules.PaymentsBilling.Application;
using Kinevor.Modules.PaymentsBilling.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kinevor.Modules.PaymentsBilling.EntityFrameworkCore;

/// <summary>EF Core payment repository.</summary>
public sealed class PaymentRepository(PaymentsBillingDbContext db) : IPaymentRepository
{
    /// <summary>Loads a payment with its refunds.</summary>
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Payments
            .Include(payment => payment.Refunds)
            .FirstOrDefaultAsync(payment => payment.Id == id, ct);

    /// <summary>Finds a payment by reference.</summary>
    public Task<Payment?> GetByReferenceAsync(string reference, CancellationToken ct = default)
    {
        var normalized = (reference ?? string.Empty).Trim().ToUpperInvariant();
        return db.Payments
            .Include(payment => payment.Refunds)
            .FirstOrDefaultAsync(payment => payment.Reference == normalized, ct);
    }

    /// <summary>Pages payments with filters.</summary>
    public async Task<(IReadOnlyList<Payment> Items, int Total)> SearchAsync(
        string? search,
        PaymentStatus? status,
        PaymentMethod? method,
        Guid? memberId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Payments.AsNoTracking().AsQueryable();

        if (memberId is { } member)
            query = query.Where(payment => payment.MemberId == member);
        if (status is { } paymentStatus)
            query = query.Where(payment => payment.Status == paymentStatus);
        if (method is { } paymentMethod)
            query = query.Where(payment => payment.Method == paymentMethod);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(payment =>
                EF.Functions.Like(payment.Reference, $"%{term}%") ||
                EF.Functions.Like(payment.MemberName, $"%{term}%") ||
                EF.Functions.Like(payment.Description, $"%{term}%") ||
                (payment.InvoiceNumber != null && EF.Functions.Like(payment.InvoiceNumber, $"%{term}%")));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <summary>Returns captured payments in a date range.</summary>
    public async Task<IReadOnlyList<Payment>> GetCapturedBetweenAsync(
        DateTime from,
        DateTime to,
        Guid? memberId,
        CancellationToken ct = default)
    {
        var query = db.Payments.AsNoTracking()
            .Where(payment => payment.PaidAt >= from && payment.PaidAt < to)
            .Where(payment => payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Failed);

        if (memberId is { } member)
            query = query.Where(payment => payment.MemberId == member);

        return await query.ToListAsync(ct);
    }

    /// <summary>Returns refunds issued in a date range.</summary>
    public async Task<IReadOnlyList<PaymentRefund>> GetRefundsBetweenAsync(
        DateTime from,
        DateTime to,
        Guid? memberId,
        CancellationToken ct = default)
    {
        var payments = db.Payments.AsNoTracking();
        if (memberId is { } member)
            payments = payments.Where(payment => payment.MemberId == member);

        return await db.PaymentRefunds
            .AsNoTracking()
            .Where(refund => refund.RefundedAt >= from && refund.RefundedAt < to)
            .Join(payments, refund => refund.PaymentId, payment => payment.Id, (refund, _) => refund)
            .ToListAsync(ct);
    }

    /// <summary>Tracks a new payment.</summary>
    public void Add(Payment payment) => db.Payments.Add(payment);
}
