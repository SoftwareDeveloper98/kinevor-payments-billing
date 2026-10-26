using Kinevor.Modules.PaymentsBilling.Domain;
using Kinevor.Platform.Application.Exceptions;
using MediatR;

namespace Kinevor.Modules.PaymentsBilling.Application;

/// <summary>Creates an invoice with line items.</summary>
public record CreateInvoiceCommand(
    Guid MemberId,
    string MemberName,
    string? MemberEmail,
    DateTime? IssuedAt,
    DateTime? DueAt,
    string Currency,
    IReadOnlyList<InvoiceLineSpec> Lines,
    string? Notes = null) : IRequest<InvoiceCreatedDto>;

/// <summary>Handles <see cref="CreateInvoiceCommand"/>.</summary>
public class CreateInvoiceHandler(
    IInvoiceRepository invoices,
    IPaymentsBillingUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<CreateInvoiceCommand, InvoiceCreatedDto>
{
    /// <summary>Builds, numbers and persists the invoice.</summary>
    public async Task<InvoiceCreatedDto> Handle(CreateInvoiceCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!string.Equals(command.Currency, BillingDefaults.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"Billing reporting supports {BillingDefaults.Currency} only.");

        var now = clock.GetUtcNow().UtcDateTime;
        var issuedAt = Normalize(command.IssuedAt) ?? now;
        if (issuedAt > now.AddDays(1))
            throw new DomainException("Issue date cannot be in the future.");
        var dueAt = Normalize(command.DueAt) ?? issuedAt.AddDays(BillingDefaults.PaymentTermDays);

        var sequence = await invoices.GetNextSequenceAsync(issuedAt.Year, ct);
        var number = Invoice.FormatNumber(issuedAt.Year, sequence);
        var result = Invoice.Create(
            number,
            command.MemberId,
            command.MemberName,
            command.MemberEmail,
            issuedAt,
            dueAt,
            BillingDefaults.Currency,
            command.Lines,
            command.Notes);

        if (result.IsFailure)
            throw new DomainException(result.Error.Message);

        var invoice = result.Value;
        invoices.Add(invoice);
        await unitOfWork.SaveChangesAsync(ct);

        return new InvoiceCreatedDto(invoice.Id, invoice.Number, invoice.Total, invoice.Currency, invoice.DueAt);
    }

    private static DateTime? Normalize(DateTime? value) =>
        value is { } date ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : null;
}

/// <summary>Marks an invoice paid outside the normal payment flow.</summary>
public record MarkInvoicePaidCommand(Guid Id) : IRequest<InvoiceDetailDto>;

/// <summary>Handles <see cref="MarkInvoicePaidCommand"/>.</summary>
public class MarkInvoicePaidHandler(
    IInvoiceRepository invoices,
    IPaymentsBillingUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<MarkInvoicePaidCommand, InvoiceDetailDto>
{
    /// <summary>Applies the paid transition and saves.</summary>
    public async Task<InvoiceDetailDto> Handle(MarkInvoicePaidCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await invoices.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Invoice", command.Id);
        var now = clock.GetUtcNow().UtcDateTime;
        var result = invoice.MarkPaid(now);
        if (result.IsFailure)
            throw new DomainException(result.Error.Message);

        await unitOfWork.SaveChangesAsync(ct);
        return BillingMapper.ToDetail(invoice, now);
    }
}

/// <summary>Voids an unpaid invoice.</summary>
public record VoidInvoiceCommand(Guid Id, string Reason) : IRequest<InvoiceDetailDto>;

/// <summary>Handles <see cref="VoidInvoiceCommand"/>.</summary>
public class VoidInvoiceHandler(
    IInvoiceRepository invoices,
    IPaymentsBillingUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<VoidInvoiceCommand, InvoiceDetailDto>
{
    /// <summary>Applies the void transition and saves.</summary>
    public async Task<InvoiceDetailDto> Handle(VoidInvoiceCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await invoices.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Invoice", command.Id);
        var now = clock.GetUtcNow().UtcDateTime;
        var result = invoice.Void(now, command.Reason);
        if (result.IsFailure)
            throw new DomainException(result.Error.Message);

        await unitOfWork.SaveChangesAsync(ct);
        return BillingMapper.ToDetail(invoice, now);
    }
}

/// <summary>Marks a past-due invoice overdue.</summary>
public record MarkInvoiceOverdueCommand(Guid Id) : IRequest<InvoiceDetailDto>;

/// <summary>Handles <see cref="MarkInvoiceOverdueCommand"/>.</summary>
public class MarkInvoiceOverdueHandler(
    IInvoiceRepository invoices,
    IPaymentsBillingUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<MarkInvoiceOverdueCommand, InvoiceDetailDto>
{
    /// <summary>Applies the overdue transition and saves.</summary>
    public async Task<InvoiceDetailDto> Handle(MarkInvoiceOverdueCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await invoices.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Invoice", command.Id);
        var now = clock.GetUtcNow().UtcDateTime;
        var result = invoice.MarkOverdue(now);
        if (result.IsFailure)
            throw new DomainException(result.Error.Message);

        await unitOfWork.SaveChangesAsync(ct);
        return BillingMapper.ToDetail(invoice, now);
    }
}
