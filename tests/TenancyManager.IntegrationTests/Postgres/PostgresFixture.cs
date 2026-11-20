using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TenancyManager.Infrastructure.Postgres;

namespace TenancyManager.IntegrationTests.Postgres;

/// <summary>
/// One throw-away PostgreSQL container per test run, migrated and seeded from the same seed files
/// as the Json provider. If Docker is unavailable the Postgres tests are skipped, unless
/// <c>REQUIRE_DOCKER=1</c> (set in CI), in which case they fail.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string? ConnectionString { get; private set; }

    public string? UnavailableReason { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();

            await using var db = CreateContext(Guid.Empty);
            await PostgresSeeder.MigrateAndSeedAsync(db, Seeds.Load());
        }
        catch (Exception ex) when (Environment.GetEnvironmentVariable("REQUIRE_DOCKER") != "1")
        {
            UnavailableReason = $"Docker/PostgreSQL unavailable: {ex.GetType().Name}: {ex.Message}";
        }
    }

    public TenancyDbContext CreateContext(Guid agencyId)
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>();
        PostgresOptions.Configure(options, ConnectionString!);
        return new TenancyDbContext(options.Options, new FixedTenant(agencyId));
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
