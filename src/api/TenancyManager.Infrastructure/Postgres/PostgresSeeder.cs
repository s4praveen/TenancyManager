using Microsoft.EntityFrameworkCore;
using TenancyManager.Infrastructure.Seed;

namespace TenancyManager.Infrastructure.Postgres;

public static class PostgresSeeder
{
    /// <summary>Applies migrations, then loads the seed files if the database has no agencies.</summary>
    public static async Task MigrateAndSeedAsync(TenancyDbContext db, SeedData seed, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Agencies.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Agencies.AddRange(seed.Agencies);
        db.Landlords.AddRange(seed.Landlords);
        db.Properties.AddRange(seed.Properties);
        await db.SaveChangesAsync(cancellationToken);
    }
}
