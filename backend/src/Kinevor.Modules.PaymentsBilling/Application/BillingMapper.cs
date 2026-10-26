using Kinevor.Modules.PaymentsBilling.Domain;

namespace Kinevor.Modules.PaymentsBilling.Application;

internal static class BillingMapper
{
    internal static InvoiceSummaryDto ToSummary(Invoice invoice, DateTime now) => new(
        invoice.Id,
        invoice.Number,
        invoice.MemberId,
        invoice.MemberName,
        invoice.MemberEmail,
        invoice.EffectiveStatus(now).ToString(),
        invoice.IssuedAt,
        invoice.DueAt,
        invoice.SubTotal,
        invoice.TaxAmount,
        invoice.Total,
        invoice.AmountPaid,
        invoice.OutstandingAmount,
        invoice.Currency,
        invoice.Lines.Count);

    internal static InvoiceDetailDto ToDetail(Invoice invoice, DateTime now) => new(
        invoice.Id,
        invoice.Number,
        invoice.MemberId,
        invoice.MemberName,
        invoice.MemberEmail,
        invoice.EffectiveStatus(now).ToString(),
        invoice.IssuedAt,
        invoice.DueAt,
        invoice.PaidAt,
        invoice.VoidedAt,
        invoice.VoidReason,
        invoice.Notes,
        invoice.SubTotal,
        invoice.TaxAmount,
        invoice.Total,
        invoice.AmountPaid,
        invoice.OutstandingAmount,
        invoice.Currency,
        invoice.Lines
            .OrderBy(line => line.CreatedAt)
            .Select(ToLine)
            .ToList());

    internal static InvoiceLineDto ToLine(InvoiceLine line) => new(
        line.Id,
        line.Description,
        line.Quantity,
        line.UnitPrice.Amount,
        line.TaxRate,
        line.Subtotal.Amount,
        line.TaxAmount.Amount,
        line.Total.Amount);

    internal static PaymentSummaryDto ToSummary(Payment payment) => new(
        payment.Id,
        payment.Reference,
        payment.MemberId,
        payment.MemberName,
        payment.InvoiceId,
        payment.InvoiceNumber,
        payment.Description,
        payment.Method.ToString(),
        payment.Status.ToString(),
        payment.Amount.Amount,
        payment.RefundedAmount.Amount,
        payment.NetAmount.Amount,
        payment.Amount.Currency,
        payment.PaidAt,
        payment.RefundedAt,
        payment.CreatedAt);

    internal static PaymentDetailDto ToDetail(Payment payment) => new(
        payment.Id,
        payment.Reference,
        payment.MemberId,
        payment.MemberName,
        payment.InvoiceId,
        payment.InvoiceNumber,
        payment.Description,
        payment.Method.ToString(),
        payment.Status.ToString(),
        payment.Amount.Amount,
        payment.RefundedAmount.Amount,
        payment.NetAmount.Amount,
        payment.Amount.Currency,
        payment.Notes,
        payment.PaidAt,
        payment.RefundedAt,
        payment.CreatedAt,
        payment.Refunds
            .OrderByDescending(refund => refund.RefundedAt)
            .Select(refund => new RefundDto(
                refund.Id,
                refund.Amount.Amount,
                refund.Reason,
                refund.RefundedAt))
            .ToList());

    internal static PaymentRecordedDto ToRecorded(Payment payment) => new(
        payment.Id,
        payment.Reference,
        payment.Status.ToString());
}
