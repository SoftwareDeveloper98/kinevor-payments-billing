using Kinevor.Platform.Application.Exceptions;
using MediatR;

namespace Kinevor.Modules.PaymentsBilling.Application;

/// <summary>Returns dashboard totals for outstanding balances and collections.</summary>
public record GetBillingStatsQuery(Guid? MemberId = null) : IRequest<BillingStatsDto>;

/// <summary>Handles <see cref="GetBillingStatsQuery"/>.</summary>
public class GetBillingStatsHandler(
    IInvoiceRepository invoices,
    IPaymentRepository payments,
    TimeProvider clock) : IRequestHandler<GetBillingStatsQuery, BillingStatsDto>
{
    /// <summary>Combines invoice balances with the current month's cash movement.</summary>
    public async Task<BillingStatsDto> Handle(GetBillingStatsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var now = clock.GetUtcNow().UtcDateTime;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var invoiceStats = await invoices.GetStatsAsync(query.MemberId, now, ct);
        var captured = await payments.GetCapturedBetweenAsync(monthStart, monthEnd, query.MemberId, ct);
        var refunds = await payments.GetRefundsBetweenAsync(monthStart, monthEnd, query.MemberId, ct);

        var collected = captured.Sum(payment => payment.Amount.Amount);
        var refunded = refunds.Sum(refund => refund.Amount.Amount);

        return new BillingStatsDto(
            invoiceStats.OpenCount,
            invoiceStats.OverdueCount,
            invoiceStats.PaidCount,
            invoiceStats.TotalCount,
            invoiceStats.OutstandingAmount,
            invoiceStats.OverdueAmount,
            collected,
            refunded,
            collected - refunded,
            BillingDefaults.Currency);
    }
}

/// <summary>Returns revenue totals and monthly buckets for a period.</summary>
public record GetRevenueSummaryQuery(DateTime? From, DateTime? To, Guid? MemberId = null)
    : IRequest<RevenueSummaryDto>;

/// <summary>Handles <see cref="GetRevenueSummaryQuery"/>.</summary>
public class GetRevenueSummaryHandler(
    IPaymentRepository payments,
    TimeProvider clock) : IRequestHandler<GetRevenueSummaryQuery, RevenueSummaryDto>
{
    /// <summary>Builds the default six-month window, buckets and totals.</summary>
    public async Task<RevenueSummaryDto> Handle(GetRevenueSummaryQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var now = clock.GetUtcNow().UtcDateTime;
        var to = Normalize(query.To) ?? now;
        var from = Normalize(query.From) ?? new DateTime(to.Year, to.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

        if (from >= to)
            throw new DomainException("Revenue period start must be before the end.");
        if (to.AddMonths(-24) > from)
            throw new DomainException("Revenue period cannot exceed 24 months.");

        var captured = await payments.GetCapturedBetweenAsync(from, to, query.MemberId, ct);
        var refunds = await payments.GetRefundsBetweenAsync(from, to, query.MemberId, ct);

        var buckets = new List<RevenueBucketDto>();
        var cursor = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        while (cursor < to)
        {
            var next = cursor.AddMonths(1);
            var gross = captured
                .Where(payment => payment.PaidAt >= cursor && payment.PaidAt < next)
                .Sum(payment => payment.Amount.Amount);
            var refunded = refunds
                .Where(refund => refund.RefundedAt >= cursor && refund.RefundedAt < next)
                .Sum(refund => refund.Amount.Amount);

            buckets.Add(new RevenueBucketDto(
                cursor.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
                cursor,
                gross,
                refunded,
                gross - refunded));
            cursor = next;
        }

        var grossTotal = captured.Sum(payment => payment.Amount.Amount);
        var refundTotal = refunds.Sum(refund => refund.Amount.Amount);

        return new RevenueSummaryDto(
            from,
            to,
            grossTotal,
            refundTotal,
            grossTotal - refundTotal,
            captured.Count,
            BillingDefaults.Currency,
            buckets);
    }

    private static DateTime? Normalize(DateTime? value) =>
        value is { } date ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : null;
}

/// <summary>Returns the outstanding summary for one member.</summary>
public record GetMemberBillingSummaryQuery(Guid MemberId) : IRequest<MemberBillingSummaryDto>;

/// <summary>Handles <see cref="GetMemberBillingSummaryQuery"/>.</summary>
public class GetMemberBillingSummaryHandler(
    IInvoiceRepository invoices,
    IPaymentRepository payments,
    TimeProvider clock) : IRequestHandler<GetMemberBillingSummaryQuery, MemberBillingSummaryDto>
{
    /// <summary>Combines current balances with net payments for the year.</summary>
    public async Task<MemberBillingSummaryDto> Handle(GetMemberBillingSummaryQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var now = clock.GetUtcNow().UtcDateTime;
        var stats = await invoices.GetStatsAsync(query.MemberId, now, ct);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);
        var captured = await payments.GetCapturedBetweenAsync(yearStart, yearEnd, query.MemberId, ct);
        var refunds = await payments.GetRefundsBetweenAsync(yearStart, yearEnd, query.MemberId, ct);

        var paid = captured.Sum(payment => payment.Amount.Amount)
            - refunds.Sum(refund => refund.Amount.Amount);

        return new MemberBillingSummaryDto(
            query.MemberId,
            stats.OutstandingAmount,
            stats.OverdueAmount,
            stats.OpenCount,
            stats.OverdueCount,
            paid,
            stats.NextDueAt,
            BillingDefaults.Currency);
    }
}
