namespace Kinevor.Modules.PaymentsBilling.Application;

/// <summary>Constants shared by the payments and billing application layer.</summary>
public static class BillingDefaults
{
    /// <summary>Single reporting currency supported by this standalone module.</summary>
    public const string Currency = "USD";

    /// <summary>Default payment terms in days.</summary>
    public const int PaymentTermDays = 14;
}

/// <summary>One line of an invoice projection.</summary>
public record InvoiceLineDto(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total);

/// <summary>Compact invoice row used by staff and member lists.</summary>
public record InvoiceSummaryDto(
    Guid Id,
    string Number,
    Guid MemberId,
    string MemberName,
    string? MemberEmail,
    string Status,
    DateTime IssuedAt,
    DateTime DueAt,
    decimal SubTotal,
    decimal TaxAmount,
    decimal Total,
    decimal AmountPaid,
    decimal OutstandingAmount,
    string Currency,
    int LineCount);

/// <summary>Full invoice projection including line items.</summary>
public record InvoiceDetailDto(
    Guid Id,
    string Number,
    Guid MemberId,
    string MemberName,
    string? MemberEmail,
    string Status,
    DateTime IssuedAt,
    DateTime DueAt,
    DateTime? PaidAt,
    DateTime? VoidedAt,
    string? VoidReason,
    string? Notes,
    decimal SubTotal,
    decimal TaxAmount,
    decimal Total,
    decimal AmountPaid,
    decimal OutstandingAmount,
    string Currency,
    IReadOnlyList<InvoiceLineDto> Lines);

/// <summary>Compact payment row.</summary>
public record PaymentSummaryDto(
    Guid Id,
    string Reference,
    Guid MemberId,
    string MemberName,
    Guid? InvoiceId,
    string? InvoiceNumber,
    string Description,
    string Method,
    string Status,
    decimal Amount,
    decimal RefundedAmount,
    decimal NetAmount,
    string Currency,
    DateTime PaidAt,
    DateTime? RefundedAt,
    DateTime CreatedAt);

/// <summary>One refund record belonging to a payment.</summary>
public record RefundDto(Guid Id, decimal Amount, string Reason, DateTime RefundedAt);

/// <summary>Full payment projection including refund history.</summary>
public record PaymentDetailDto(
    Guid Id,
    string Reference,
    Guid MemberId,
    string MemberName,
    Guid? InvoiceId,
    string? InvoiceNumber,
    string Description,
    string Method,
    string Status,
    decimal Amount,
    decimal RefundedAmount,
    decimal NetAmount,
    string Currency,
    string? Notes,
    DateTime PaidAt,
    DateTime? RefundedAt,
    DateTime CreatedAt,
    IReadOnlyList<RefundDto> Refunds);

/// <summary>Outstanding and collection statistics for the dashboard.</summary>
public record BillingStatsDto(
    int OpenInvoiceCount,
    int OverdueInvoiceCount,
    int PaidInvoiceCount,
    int TotalInvoiceCount,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    decimal CollectedThisMonth,
    decimal RefundedThisMonth,
    decimal NetThisMonth,
    string Currency);

/// <summary>One bucket in the revenue chart.</summary>
public record RevenueBucketDto(string Label, DateTime PeriodStart, decimal GrossCollected, decimal Refunded, decimal NetRevenue);

/// <summary>Revenue totals and monthly buckets for a reporting period.</summary>
public record RevenueSummaryDto(
    DateTime From,
    DateTime To,
    decimal GrossCollected,
    decimal Refunded,
    decimal NetRevenue,
    int PaymentCount,
    string Currency,
    IReadOnlyList<RevenueBucketDto> Buckets);

/// <summary>Outstanding summary for the authenticated member.</summary>
public record MemberBillingSummaryDto(
    Guid MemberId,
    decimal OutstandingAmount,
    decimal OverdueAmount,
    int OpenInvoiceCount,
    int OverdueInvoiceCount,
    decimal PaidThisYear,
    DateTime? NextDueAt,
    string Currency);

/// <summary>Result of creating an invoice.</summary>
public record InvoiceCreatedDto(Guid Id, string Number, decimal Total, string Currency, DateTime DueAt);

/// <summary>Result of recording a payment.</summary>
public record PaymentRecordedDto(Guid Id, string Reference, string Status);
