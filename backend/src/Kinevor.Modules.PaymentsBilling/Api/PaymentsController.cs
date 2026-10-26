using Kinevor.Modules.PaymentsBilling.Application;
using Kinevor.Modules.PaymentsBilling.Domain;
using Kinevor.Platform.Api;
using Kinevor.Platform.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kinevor.Modules.PaymentsBilling.Api;

/// <summary>Staff payment and refund endpoints.</summary>
[ApiController]
[Route("api/v1/billing/payments")]
[Authorize(Policy = Policies.StaffOrAdmin)]
public class PaymentsController(ISender sender) : ControllerBase
{
    /// <summary>Lists payments with optional search, status, method and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PaymentSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentSummaryDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] PaymentStatus? status,
        [FromQuery] PaymentMethod? method,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPaymentsQuery(search, status, method, null, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<PaymentSummaryDto>>.Ok(result));
    }

    /// <summary>Returns a payment with its refund history.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaymentDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetPaymentByIdQuery(id), ct);
        return Ok(ApiResponse<PaymentDetailDto>.Ok(result));
    }

    /// <summary>Records a simulated card, cash or bank-transfer payment.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentRecordedDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PaymentRecordedDto>>> Record(
        [FromBody] RecordPaymentRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new RecordPaymentCommand(
            request.MemberId,
            request.MemberName ?? string.Empty,
            request.InvoiceId,
            request.Amount,
            string.IsNullOrWhiteSpace(request.Currency) ? BillingDefaults.Currency : request.Currency,
            request.Method,
            request.Reference,
            request.Description,
            request.Notes), ct);

        return Created($"/api/v1/billing/payments/{result.Id}",
            ApiResponse<PaymentRecordedDto>.Ok(result, "Payment recorded."));
    }

    /// <summary>Refunds all or part of a captured payment.</summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaymentDetailDto>>> Refund(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new RefundPaymentCommand(id, request.Amount, request.Reason), ct);
        return Ok(ApiResponse<PaymentDetailDto>.Ok(result, "Refund recorded."));
    }
}

/// <summary>Payment creation request body.</summary>
public record RecordPaymentRequest(
    Guid MemberId,
    string? MemberName,
    Guid? InvoiceId,
    decimal Amount,
    string? Currency,
    PaymentMethod Method,
    string? Reference,
    string? Description,
    string? Notes = null);

/// <summary>Refund request body.</summary>
public record RefundPaymentRequest(decimal Amount, string Reason);
