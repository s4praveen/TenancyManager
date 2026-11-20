using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TenancyManager.Application.Common;

namespace TenancyManager.Api.Endpoints;

internal static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                var provider = context.RequestServices.GetRequiredService<IDataProviderInfo>();
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    new HealthResponse(report.Status.ToString(), provider.Name),
                    JsonSerializerOptions.Web);
            },
        }).AllowAnonymous();

        return app;
    }

    internal sealed record HealthResponse(string Status, string DataProvider);
}
