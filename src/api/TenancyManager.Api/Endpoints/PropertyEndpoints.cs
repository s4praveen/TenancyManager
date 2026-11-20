using Microsoft.AspNetCore.Http.HttpResults;
using TenancyManager.Application.Properties;
using TenancyManager.Domain.Common;

namespace TenancyManager.Api.Endpoints;

internal static class PropertyEndpoints
{
    public static RouteGroupBuilder MapProperties(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/properties").WithTags("Properties");

        group.MapGet("/", async (IPropertyRepository repo, CancellationToken ct) =>
                TypedResults.Ok((await repo.ListAsync(ct)).Select(PropertyResponse.From).ToList()))
            .WithName("ListProperties")
            .WithSummary("The current agency's properties, ordered by town then address (PRP-001).");

        group.MapGet("/{id:guid}", async Task<Results<Ok<PropertyResponse>, NotFound>> (Guid id, IPropertyRepository repo, CancellationToken ct) =>
                await repo.GetAsync(id, ct) is { } p ? TypedResults.Ok(PropertyResponse.From(p)) : TypedResults.NotFound())
            .WithName("GetProperty")
            .WithSummary("A single property. 404 for unknown ids and other agencies' properties (PRP-003, AGY-002).");

        return api;
    }
}

public sealed record PropertyResponse(
    Guid Id,
    string Line1,
    string? Line2,
    string Town,
    string Postcode,
    Jurisdiction Jurisdiction,
    int Bedrooms,
    Guid LandlordId,
    string LandlordName)
{
    internal static PropertyResponse From(PropertySummary p) =>
        new(p.Id, p.Line1, p.Line2, p.Town, p.Postcode, p.Jurisdiction, p.Bedrooms, p.LandlordId, p.LandlordName);
}
