using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using TenancyManager.Api.Auth;
using TenancyManager.Application.Agencies;

namespace TenancyManager.Api.Endpoints;

internal static class MeEndpoints
{
    public static RouteGroupBuilder MapMe(this RouteGroupBuilder api)
    {
        api.MapGet("/me", GetMe)
            .WithName("GetCurrentUser")
            .WithTags("Identity")
            .WithSummary("The signed-in user, their roles and current agency (IAM-003).");
        return api;
    }

    private static async Task<Results<Ok<MeResponse>, ProblemHttpResult>> GetMe(
        ClaimsPrincipal user, IAgencyRepository agencies, CancellationToken ct)
    {
        var agency = await agencies.GetCurrentAsync(ct);
        if (agency is null)
        {
            return TypedResults.Problem("The configured agency does not exist.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var userName = user.FindFirstValue(AgencyRoles.UserNameClaimType) ?? user.Identity?.Name ?? string.Empty;
        var roles = user.FindAll(AgencyRoles.RoleClaimType).Select(c => c.Value).Distinct().Order().ToArray();

        return TypedResults.Ok(new MeResponse(
            userName,
            user.FindFirstValue(AgencyRoles.NameClaimType) ?? userName,
            roles,
            new AgencyResponse(agency.Id, agency.Name)));
    }
}

public sealed record MeResponse(string UserName, string DisplayName, IReadOnlyList<string> Roles, AgencyResponse Agency);

public sealed record AgencyResponse(Guid Id, string Name);
