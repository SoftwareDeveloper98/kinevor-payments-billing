using Kinevor.Modules.PaymentsBilling.Application;
using Kinevor.Modules.PaymentsBilling.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kinevor.Modules.PaymentsBilling.EntityFrameworkCore;

/// <summary>EF Core invoice repository.</summary>
public sealed class InvoiceRepository(PaymentsBillingDbContext db) : IInvoiceRepository
{
    /// <summary>Loads an invoice with its line items.</summary>
    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Invoices
            .Include(invoice => invoice.Lines)
            .FirstOrDefaultAsync(invoice => invoice.Id == id, ct);

    /// <summary>Finds an invoice by number.</summary>
    public Task<Invoice?> GetByNumberAsync(string number, CancellationToken ct = default)
    {
        var normalized = (number ?? string.Empty).Trim().ToUpperInvariant();
        return db.Invoices
            .Include(invoice => invoice.Lines)
            .FirstOrDefaultAsync(invoice => invoice.Number == normalized, ct);
    }

    /// <summary>Pages invoices with effective-status filtering.</summary>
    public async Task<(IReadOnlyList<Invoice> Items, int Total)> SearchAsync(
        string? search,
        InvoiceStatus? status,
        Guid? memberId,
        DateTime now,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Invoices.AsNoTracking().Include(invoice => invoice.Lines).AsQueryable();

        if (memberId is { } member)
            query = query.Where(invoice => invoice.MemberId == member);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(invoice =>
                EF.Functions.Like(invoice.Number, $"%{term}%") ||
                EF.Functions.Like(invoice.MemberName, $"%{term}%") ||
                (invoice.MemberEmail != null && EF.Functions.Like(invoice.MemberEmail, $"%{term}%")));
        }

        query = status switch
        {
            InvoiceStatus.Open => query.Where(invoice =>
                invoice.Status == InvoiceStatus.Open && invoice.DueAt >= now.Date),
            InvoiceStatus.Overdue => query.Where(invoice =>
                invoice.Status == InvoiceStatus.Overdue ||
                (invoice.Status == InvoiceStatus.Open && invoice.DueAt < now.Date)),
            InvoiceStatus.Paid => query.Where(invoice => invoice.Status == InvoiceStatus.Paid),
            InvoiceStatus.Void => query.Where(invoice => invoice.Status == InvoiceStatus.Void),
            _ => query,
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(invoice => invoice.IssuedAt)
            .ThenByDescending(invoice => invoice.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <summary>Returns the next sequence for a year from the highest existing number.</summary>
    public async Task<int> GetNextSequenceAsync(int year, CancellationToken ct = default)
    {
        var prefix = $"INV-{year}-";
        var last = await db.Invoices
            .Where(invoice => invoice.Number.StartsWith(prefix))
            .OrderByDescending(invoice => invoice.Number)
            .Select(invoice => invoice.Number)
            .FirstOrDefaultAsync(ct);

        if (last is null)
            return 1;

        var suffix = last[prefix.Length..];
        return int.TryParse(suffix, out var sequence) ? sequence + 1 : 1;
    }

    /// <summary>Counts unpaid and paid invoices and sums their balances.</summary>
    public async Task<InvoiceStatsSnapshot> GetStatsAsync(
        Guid? memberId,
        DateTime now,
        CancellationToken ct = default)
    {
        var query = db.Invoices.AsNoTracking().AsQueryable();
        if (memberId is { } member)
            query = query.Where(invoice => invoice.MemberId == member);

        var totalCount = await query.CountAsync(ct);
        var paidCount = await query.CountAsync(invoice => invoice.Status == InvoiceStatus.Paid, ct);
        var unpaid = await query
            .Where(invoice => invoice.Status == InvoiceStatus.Open || invoice.Status == InvoiceStatus.Overdue)
            .Select(invoice => new
            {
                invoice.Status,
                invoice.Total,
                invoice.AmountPaid,
                invoice.DueAt,
            })
            .ToListAsync(ct);

        var overdue = unpaid
            .Where(invoice => invoice.Status == InvoiceStatus.Overdue || invoice.DueAt < now.Date)
            .ToList();
        var outstanding = unpaid.Sum(invoice => Math.Max(0m, invoice.Total - invoice.AmountPaid));
        var overdueAmount = overdue.Sum(invoice => Math.Max(0m, invoice.Total - invoice.AmountPaid));
        var nextDue = unpaid.Count == 0 ? (DateTime?)null : unpaid.Min(invoice => invoice.DueAt);

        return new InvoiceStatsSnapshot(
            unpaid.Count,
            overdue.Count,
            paidCount,
            totalCount,
            outstanding,
            overdueAmount,
            nextDue);
    }

    /// <summary>Tracks a new invoice.</summary>
    public void Add(Invoice invoice) => db.Invoices.Add(invoice);
}
