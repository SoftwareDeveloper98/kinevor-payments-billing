using Kinevor.Modules.PaymentsBilling.Application;
using Kinevor.Platform.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kinevor.Modules.PaymentsBilling.Api;

/// <summary>Staff revenue and billing dashboard endpoints.</summary>
[ApiController]
[Route("api/v1/billing")]
[Authorize(Policy = Policies.StaffOrAdmin)]
public class BillingController(ISender sender) : ControllerBase
{
    /// <summary>Returns outstanding balances and current-month collections.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<BillingStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BillingStatsDto>>> Stats(CancellationToken ct)
    {
        var result = await sender.Send(new GetBillingStatsQuery(), ct);
        return Ok(ApiResponse<BillingStatsDto>.Ok(result));
    }

    /// <summary>Returns revenue totals and monthly buckets for the requested period.</summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(ApiResponse<RevenueSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RevenueSummaryDto>>> Revenue(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetRevenueSummaryQuery(from, to), ct);
        return Ok(ApiResponse<RevenueSummaryDto>.Ok(result));
    }
}
