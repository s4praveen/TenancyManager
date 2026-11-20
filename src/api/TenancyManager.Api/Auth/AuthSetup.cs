using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace TenancyManager.Api.Auth;

public static class AuthSetup
{
    public const string DevMode = "Dev";
    public const string EntraMode = "Entra";

    public static WebApplicationBuilder AddTenancyAuth(this WebApplicationBuilder builder)
    {
        var mode = builder.Configuration["Auth:Mode"] ?? EntraMode;

        if (string.Equals(mode, DevMode, StringComparison.OrdinalIgnoreCase))
        {
            if (builder.Environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "Dev authentication is not allowed in Production. Set Auth:Mode to 'Entra'.");
            }

            builder.Services
                .AddAuthentication(DevAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, null);
        }
        else if (string.Equals(mode, EntraMode, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

            // Keep raw Entra claim names ("roles", "name") so Entra and Dev principals look identical.
            builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters.RoleClaimType = AgencyRoles.RoleClaimType;
                o.TokenValidationParameters.NameClaimType = AgencyRoles.NameClaimType;
            });
        }
        else
        {
            throw new InvalidOperationException($"Unsupported Auth:Mode '{mode}'. Use '{EntraMode}' or '{DevMode}'.");
        }

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AgencyRoles.AgencyUserPolicy, p => p.RequireAuthenticatedUser().RequireRole(AgencyRoles.All));

        return builder;
    }
}
