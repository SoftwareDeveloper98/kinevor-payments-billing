using Kinevor.Modules.PaymentsBilling.Application;
using Kinevor.Modules.PaymentsBilling.Domain;
using Kinevor.Platform.Api;
using Kinevor.Platform.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kinevor.Modules.PaymentsBilling.Api;

/// <summary>Staff invoice management endpoints.</summary>
[ApiController]
[Route("api/v1/billing/invoices")]
[Authorize(Policy = Policies.StaffOrAdmin)]
public class InvoicesController(ISender sender) : ControllerBase
{
    /// <summary>Lists invoices with optional search, status and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InvoiceSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceSummaryDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetInvoicesQuery(search, status, null, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<InvoiceSummaryDto>>.Ok(result));
    }

    /// <summary>Returns an invoice with its line items.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(id), ct);
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(result));
    }

    /// <summary>Creates an invoice with line items.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvoiceCreatedDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<InvoiceCreatedDto>>> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken ct)
    {
        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? BillingDefaults.Currency
            : request.Currency;
        var lines = request.Lines
            .Select(line => new InvoiceLineSpec(line.Description, line.Quantity, line.UnitPrice, line.TaxRate))
            .ToList();

        var created = await sender.Send(new CreateInvoiceCommand(
            request.MemberId,
            request.MemberName,
            request.MemberEmail,
            request.IssuedAt,
            request.DueAt,
            currency,
            lines,
            request.Notes), ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<InvoiceCreatedDto>.Ok(created, "Invoice created."));
    }

    /// <summary>Marks an invoice paid outside the payment flow.</summary>
    [HttpPost("{id:guid}/paid")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> MarkPaid(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkInvoicePaidCommand(id), ct);
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(result, "Invoice marked paid."));
    }

    /// <summary>Voids an unpaid invoice.</summary>
    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> Void(
        Guid id,
        [FromBody] VoidInvoiceRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new VoidInvoiceCommand(id, request.Reason), ct);
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(result, "Invoice voided."));
    }

    /// <summary>Marks a past-due invoice overdue.</summary>
    [HttpPost("{id:guid}/overdue")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> MarkOverdue(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkInvoiceOverdueCommand(id), ct);
        return Ok(ApiResponse<InvoiceDetailDto>.Ok(result, "Invoice marked overdue."));
    }
}

/// <summary>Invoice creation request body.</summary>
public record CreateInvoiceRequest(
    Guid MemberId,
    string MemberName,
    string? MemberEmail,
    DateTime? IssuedAt,
    DateTime? DueAt,
    string? Currency,
    IReadOnlyList<InvoiceLineInput> Lines,
    string? Notes = null);

/// <summary>Invoice line input.</summary>
public record InvoiceLineInput(string Description, int Quantity, decimal UnitPrice, decimal TaxRate);

/// <summary>Void request body.</summary>
public record VoidInvoiceRequest(string Reason);
