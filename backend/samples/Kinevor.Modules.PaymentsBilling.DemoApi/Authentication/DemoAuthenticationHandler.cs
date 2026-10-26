using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Kinevor.Modules.PaymentsBilling.DemoApi.Authentication;

/// <summary>
/// Demonstration authentication handler that trusts the <c>X-Demo-Role</c> header.
/// It exists only to make the standalone sample runnable without an identity provider.
/// </summary>
public sealed class DemoAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IWebHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Authentication scheme used by the demo host.</summary>
    public const string SchemeName = "Demo";

    private static readonly HashSet<string> AllowedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Admin", "Staff", "Trainer", "Member" };

    /// <summary>Builds a principal from the demo headers, or returns no result when they are absent.</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!string.Equals(environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Demo authentication is disabled outside Development."));

        var role = Request.Headers["X-Demo-Role"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(role))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AllowedRoles.Contains(role))
            return Task.FromResult(AuthenticateResult.Fail($"Unknown demo role '{role}'."));

        var subject = Request.Headers["X-Demo-User"].FirstOrDefault() ?? $"demo-{role.ToLowerInvariant()}";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subject),
            new(ClaimTypes.Role, role),
        };

        AddIfPresent(claims, ClaimTypes.Email, Request.Headers["X-Demo-Email"].FirstOrDefault());
        AddGuidClaim(claims, "trainer_id", Request.Headers["X-Demo-Trainer"].FirstOrDefault());
        AddGuidClaim(claims, "branch_id", Request.Headers["X-Demo-Branch"].FirstOrDefault());
        AddGuidClaim(claims, "member_id", Request.Headers["X-Demo-Member"].FirstOrDefault());

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static void AddIfPresent(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            claims.Add(new Claim(type, value));
    }

    private static void AddGuidClaim(List<Claim> claims, string type, string? value)
    {
        if (Guid.TryParse(value, out var id))
            claims.Add(new Claim(type, id.ToString()));
    }
}
