using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TenancyManager.Application.Agencies;
using TenancyManager.Application.Common;
using TenancyManager.Application.Properties;
using TenancyManager.Infrastructure.Json;
using TenancyManager.Infrastructure.Postgres;
using TenancyManager.Infrastructure.Seed;
using TenancyManager.Infrastructure.Tenancy;

namespace TenancyManager.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the configured data provider (DAT-001). Throws on an unknown provider so that a
    /// misconfiguration fails at start-up, not on the first request.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var options = configuration.GetSection(DataProviderOptions.SectionName).Get<DataProviderOptions>()
            ?? new DataProviderOptions();

        var provider = DataProviderOptions.Supported
            .FirstOrDefault(p => string.Equals(p, options.Type, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Unsupported data provider '{options.Type}'. Supported providers: {string.Join(", ", DataProviderOptions.Supported)}.");

        services.AddSingleton<ITenantContext, ConfigurationTenantContext>();
        services.AddSingleton<IDataProviderInfo>(new DataProviderInfo(provider));

        var seedRoot = SeedReader.ResolveRoot(options.SeedPath, contentRootPath);
        services.AddSingleton(_ => SeedReader.Read(seedRoot));

        var health = services.AddHealthChecks();

        if (provider == DataProviderOptions.Json)
        {
            services.AddSingleton<JsonDataStore>();
            services.AddScoped<IAgencyRepository, JsonAgencyRepository>();
            services.AddScoped<IPropertyRepository, JsonPropertyRepository>();
            health.AddCheck("data-store", () => HealthCheckResult.Healthy());
        }
        else
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is required when DataProvider:Type is Postgres.");

            services.AddDbContext<TenancyDbContext>(o => PostgresOptions.Configure(o, connectionString));
            services.AddScoped<IAgencyRepository, PostgresAgencyRepository>();
            services.AddScoped<IPropertyRepository, PostgresPropertyRepository>();
            health.AddDbContextCheck<TenancyDbContext>("data-store");

            if (options.MigrateOnStartup)
            {
                services.AddHostedService<PostgresInitializer>();
            }
        }

        return services;
    }

    private sealed record DataProviderInfo(string Name) : IDataProviderInfo;
}
