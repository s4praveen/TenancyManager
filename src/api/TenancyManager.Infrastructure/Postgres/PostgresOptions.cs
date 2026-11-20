using Microsoft.EntityFrameworkCore;

namespace TenancyManager.Infrastructure.Postgres;

public static class PostgresOptions
{
    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention();
}
