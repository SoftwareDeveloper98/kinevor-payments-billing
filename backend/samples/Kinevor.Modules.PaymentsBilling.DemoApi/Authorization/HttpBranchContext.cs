using System.Security.Claims;
using Kinevor.Platform.Application.Common;

namespace Kinevor.Modules.PaymentsBilling.DemoApi.Authorization;

/// <summary>Resolves the current branch from the user claim or the demo header.</summary>
public sealed class HttpBranchContext(IHttpContextAccessor accessor) : IBranchContext
{
    /// <summary>Branch the current request is scoped to, when any.</summary>
    public Guid? BranchId
    {
        get
        {
            var context = accessor.HttpContext;
            if (context is null) return null;

            if (Guid.TryParse(context.User.FindFirstValue("branch_id"), out var fromClaim))
                return fromClaim;

            return Guid.TryParse(context.Request.Headers["X-Branch-Id"].FirstOrDefault(), out var fromHeader)
                ? fromHeader
                : null;
        }
    }
}
