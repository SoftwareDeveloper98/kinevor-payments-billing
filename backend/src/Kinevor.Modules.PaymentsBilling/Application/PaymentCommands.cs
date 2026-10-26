using System.Security.Cryptography;
using Kinevor.Modules.PaymentsBilling.Domain;
using Kinevor.Modules.PaymentsBilling.Domain.ValueObjects;
using Kinevor.Platform.Application.Exceptions;
using MediatR;

namespace Kinevor.Modules.PaymentsBilling.Application;

/// <summary>Records a simulated card, cash or bank-transfer payment.</summary>
public record RecordPaymentCommand(
    Guid MemberId,
    string MemberName,
    Guid? InvoiceId,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string? Reference,
    string? Description,
    string? Notes = null) : IRequest<PaymentRecordedDto>;

/// <summary>Handles <see cref="RecordPaymentCommand"/> with reference-level idempotency.</summary>
public class RecordPaymentHandler(
    IInvoiceRepository invoices,
    IPaymentRepository payments,
    IPaymentsBillingUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<RecordPaymentCommand, PaymentRecordedDto>
{
    /// <summary>Validates the invoice, applies the payment and persists it.</summary>
    public async Task<PaymentRecordedDto> Handle(RecordPaymentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!string.Equals(command.Currency, BillingDefaults.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"Billing reporting supports {BillingDefaults.Currency} only.");
        if (command.Amount <= 0m)
            throw new DomainException("Payment amount must be positive.");

        var now = clock.GetUtcNow().UtcDateTime;
        var reference = string.IsNullOrWhiteSpace(command.Reference)
            ? GenerateReference(now)
            : command.Reference.Trim().ToUpperInvariant();

        var existing = await payments.GetByReferenceAsync(reference, ct);
        if (existing is not null)
        {
            var sameRequest = existing.MemberId == command.MemberId
                && existing.InvoiceId == command.InvoiceId
                && existing.Amount.Amount == command.Amount
                && existing.Method == command.Method;
            if (!sameRequest)
                throw new ConflictException($"Payment reference '{reference}' is already used by another request.");
            return BillingMapper.ToRecorded(existing);
        }

        Invoice? invoice = null;
        if (command.InvoiceId is { } invoiceId)
        {
            invoice = await invoices.GetByIdAsync(invoiceId, ct)
                ?? throw new NotFoundException("Invoice", invoiceId);
            if (invoice.MemberId != command.MemberId)
                throw new DomainException("The invoice belongs to a different member.");
            if (!invoice.IsPayable)
                throw new DomainException($"A {invoice.EffectiveStatus(now)} invoice cannot accept a payment.");
            if (command.Amount > invoice.OutstandingAmount)
                throw new DomainException(
                    $"Payment exceeds the outstanding balance of {invoice.Currency} {invoice.OutstandingAmount:F2}.");
        }

        var memberName = invoice?.MemberName ?? command.MemberName;
        var description = string.IsNullOrWhiteSpace(command.Description)
            ? invoice is null ? "Simulated payment" : $"Payment for {invoice.Number}"
            : command.Description.Trim();

        var result = Payment.Record(
            command.MemberId,
            memberName,
            invoice?.Id,
            invoice?.Number,
            description,
            command.Amount,
            BillingDefaults.Currency,
            command.Method,
            reference,
            now,
            command.Notes);

        if (result.IsFailure)
            throw new DomainException(result.Error.Message);

        var payment = result.Value;
        if (invoice is not null)
        {
            var applied = invoice.ApplyPayment(command.Amount, now);
            if (applied.IsFailure)
                throw new DomainException(applied.Error.Message);
        }

        payments.Add(payment);
        await unitOfWork.SaveChangesAsync(ct);
        return BillingMapper.ToRecorded(payment);
    }

    private static string GenerateReference(DateTime now) =>
        $"SIM-{now:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
}

/// <summary>Refunds all or part of a captured payment.</summary>
public record RefundPaymentCommand(Guid PaymentId, decimal Amount, string Reason) : IRequest<PaymentDetailDto>;

/// <summary>Handles <see cref="RefundPaymentCommand"/>.</summary>
public class RefundPaymentHandler(
    IPaymentRepository payments,
    IPaymentsBillingUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<RefundPaymentCommand, PaymentDetailDto>
{
    /// <summary>Validates the refund and persists it.</summary>
    public async Task<PaymentDetailDto> Handle(RefundPaymentCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var payment = await payments.GetByIdAsync(command.PaymentId, ct)
            ?? throw new NotFoundException("Payment", command.PaymentId);
        var money = Money.Create(command.Amount, payment.Amount.Currency);
        if (money.IsFailure)
            throw new DomainException(money.Error.Message);

        var result = payment.Refund(money.Value, command.Reason, clock.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
            throw new DomainException(result.Error.Message);

        await unitOfWork.SaveChangesAsync(ct);
        return BillingMapper.ToDetail(payment);
    }
}
