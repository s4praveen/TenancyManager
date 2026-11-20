using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenancyManager.Infrastructure.Seed;

namespace TenancyManager.Infrastructure.Postgres;

/// <summary>Runs before the web server starts listening, so requests never see an unmigrated database.</summary>
internal sealed class PostgresInitializer(IServiceScopeFactory scopes, SeedData seed) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        await PostgresSeeder.MigrateAndSeedAsync(db, seed, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
