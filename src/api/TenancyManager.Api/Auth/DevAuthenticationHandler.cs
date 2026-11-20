using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TenancyManager.Api.Auth;

/// <summary>
/// Local development and test authentication: trusts the <c>X-Dev-User</c> and <c>X-Dev-Roles</c> headers.
/// Requests without <c>X-Dev-User</c> are anonymous. Never enabled in Production (IAM-004).
/// </summary>
internal sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Dev";
    public const string UserHeader = "X-Dev-User";
    public const string RolesHeader = "X-Dev-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers[UserHeader].ToString();
        if (string.IsNullOrWhiteSpace(user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(AgencyRoles.NameClaimType, user),
            new(AgencyRoles.UserNameClaimType, user),
        };
        claims.AddRange(Request.Headers[RolesHeader].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(role => new Claim(AgencyRoles.RoleClaimType, role)));

        var identity = new ClaimsIdentity(claims, SchemeName, AgencyRoles.NameClaimType, AgencyRoles.RoleClaimType);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
