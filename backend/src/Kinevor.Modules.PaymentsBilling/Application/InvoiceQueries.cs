using Kinevor.Modules.PaymentsBilling.Domain;
using Kinevor.Platform.Application.Common;
using Kinevor.Platform.Application.Exceptions;
using MediatR;

namespace Kinevor.Modules.PaymentsBilling.Application;

/// <summary>Lists invoices with search, status and paging.</summary>
public record GetInvoicesQuery(
    string? Search,
    InvoiceStatus? Status,
    Guid? MemberId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<InvoiceSummaryDto>>;

/// <summary>Handles <see cref="GetInvoicesQuery"/>.</summary>
public class GetInvoicesHandler(IInvoiceRepository invoices, TimeProvider clock)
    : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceSummaryDto>>
{
    /// <summary>Returns the requested page of invoice summaries.</summary>
    public async Task<PagedResult<InvoiceSummaryDto>> Handle(GetInvoicesQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var now = clock.GetUtcNow().UtcDateTime;
        var (items, total) = await invoices.SearchAsync(
            query.Search, query.Status, query.MemberId, now, page, pageSize, ct);

        return new PagedResult<InvoiceSummaryDto>(
            items.Select(invoice => BillingMapper.ToSummary(invoice, now)).ToList(),
            total,
            page,
            pageSize);
    }
}

/// <summary>Loads one invoice, optionally constrained to the authenticated member.</summary>
public record GetInvoiceByIdQuery(Guid Id, Guid? RequiredMemberId = null) : IRequest<InvoiceDetailDto>;

/// <summary>Handles <see cref="GetInvoiceByIdQuery"/>.</summary>
public class GetInvoiceByIdHandler(IInvoiceRepository invoices, TimeProvider clock)
    : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto>
{
    /// <summary>Loads the invoice and checks member ownership.</summary>
    public async Task<InvoiceDetailDto> Handle(GetInvoiceByIdQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoice = await invoices.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException("Invoice", query.Id);

        if (query.RequiredMemberId is { } memberId && invoice.MemberId != memberId)
            throw new NotFoundException("Invoice", query.Id);

        return BillingMapper.ToDetail(invoice, clock.GetUtcNow().UtcDateTime);
    }
}

/// <summary>Lists payments with search, status and paging.</summary>
public record GetPaymentsQuery(
    string? Search,
    PaymentStatus? Status,
    PaymentMethod? Method,
    Guid? MemberId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PaymentSummaryDto>>;

/// <summary>Handles <see cref="GetPaymentsQuery"/>.</summary>
public class GetPaymentsHandler(IPaymentRepository payments)
    : IRequestHandler<GetPaymentsQuery, PagedResult<PaymentSummaryDto>>
{
    /// <summary>Returns the requested page of payment summaries.</summary>
    public async Task<PagedResult<PaymentSummaryDto>> Handle(GetPaymentsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await payments.SearchAsync(
            query.Search, query.Status, query.Method, query.MemberId, page, pageSize, ct);

        return new PagedResult<PaymentSummaryDto>(
            items.Select(BillingMapper.ToSummary).ToList(),
            total,
            page,
            pageSize);
    }
}

/// <summary>Loads one payment, optionally constrained to the authenticated member.</summary>
public record GetPaymentByIdQuery(Guid Id, Guid? RequiredMemberId = null) : IRequest<PaymentDetailDto>;

/// <summary>Handles <see cref="GetPaymentByIdQuery"/>.</summary>
public class GetPaymentByIdHandler(IPaymentRepository payments)
    : IRequestHandler<GetPaymentByIdQuery, PaymentDetailDto>
{
    /// <summary>Loads the payment and checks member ownership.</summary>
    public async Task<PaymentDetailDto> Handle(GetPaymentByIdQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var payment = await payments.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException("Payment", query.Id);

        if (query.RequiredMemberId is { } memberId && payment.MemberId != memberId)
            throw new NotFoundException("Payment", query.Id);

        return BillingMapper.ToDetail(payment);
    }
}
