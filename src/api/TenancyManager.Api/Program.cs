using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using TenancyManager.Api.Auth;
using TenancyManager.Api.Endpoints;
using TenancyManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddTenancyAuth();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict; // integers stay integers in OpenAPI
});
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "TenancyManager API";
    doc.Info.Version = "v1";
    return Task.CompletedTask;
}));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealth();

app.MapGroup("/api")
    .RequireAuthorization(AgencyRoles.AgencyUserPolicy)
    .MapMe()
    .MapProperties();

await app.RunAsync();

/// <summary>Entry point marker for WebApplicationFactory in acceptance tests.</summary>
public partial class Program;
