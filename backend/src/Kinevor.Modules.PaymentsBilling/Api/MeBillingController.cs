using Kinevor.Modules.PaymentsBilling.Application;
using Kinevor.Modules.PaymentsBilling.Domain;
using Kinevor.Platform.Api;
using Kinevor.Platform.Application.Common;
using Kinevor.Platform.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kinevor.Modules.PaymentsBilling.Api;

/// <summary>Member self-service invoice and payment endpoints.</summary>
[ApiController]
[Route("api/v1/me")]
[Authorize(Policy = Policies.MemberOrAbove)]
public class MeBillingController(ISender sender, ICurrentUser currentUser, IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>Lists the authenticated member's invoices.</summary>
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InvoiceSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceSummaryDto>>>> Invoices(
        [FromQuery] string? search,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetInvoicesQuery(search, status, MemberId, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<InvoiceSummaryDto>>.Ok(result));
    }

    /// <summary>Returns the authenticated member's outstanding summary.</summary>
    [HttpGet("invoices/summary")]
    [ProducesResponseType(typeof(ApiResponse<MemberBillingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MemberBillingSummaryDto>>> Summary(CancellationToken ct)
    {
        var result = await sender.Send(new GetMemberBillingSummaryQuery(MemberId), ct);
        return Ok(ApiResponse<MemberBillingSummaryDto>.Ok(result));
    }

    /// <summary>Returns one of the authenticated member's invoices.</summary>
    [HttpGet("invoices/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> Invoice(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(id, MemberId), ct);
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(result));
    }

    /// <summary>Pays one of the authenticated member's invoices with the simulated gateway.</summary>
    /// <remarks>Demo-only. Disabled outside Development; connect a real payment gateway for production.</remarks>
    [HttpPost("invoices/{id:guid}/pay")]
    [ProducesResponseType(typeof(ApiResponse<PaymentRecordedDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PaymentRecordedDto>>> Pay(
        Guid id,
        [FromBody] MemberPayInvoiceRequest request,
        CancellationToken ct)
    {
        if (!string.Equals(environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden, "Member self-pay is a demo simulation; connect a payment gateway for production.");

        var invoice = await sender.Send(new GetInvoiceByIdQuery(id, MemberId), ct);
        var amount = request.Amount ?? invoice.OutstandingAmount;

        var result = await sender.Send(new RecordPaymentCommand(
            MemberId,
            invoice.MemberName,
            id,
            amount,
            BillingDefaults.Currency,
            request.Method,
            request.Reference,
            null,
            null), ct);

        return Created($"/api/v1/me/payments/{result.Id}",
            ApiResponse<PaymentRecordedDto>.Ok(result, "Payment recorded."));
    }

    /// <summary>Lists the authenticated member's payments.</summary>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PaymentSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentSummaryDto>>>> Payments(
        [FromQuery] string? search,
        [FromQuery] PaymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPaymentsQuery(search, status, null, MemberId, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<PaymentSummaryDto>>.Ok(result));
    }

    /// <summary>Returns one of the authenticated member's payments.</summary>
    [HttpGet("payments/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaymentDetailDto>>> Payment(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetPaymentByIdQuery(id, MemberId), ct);
        return Ok(ApiResponse<PaymentDetailDto>.Ok(result));
    }

    private Guid MemberId =>
        currentUser.MemberId ?? throw new ForbiddenException("No linked member profile.");
}

/// <summary>Member invoice payment request body.</summary>
public record MemberPayInvoiceRequest(decimal? Amount, PaymentMethod Method, string? Reference);
