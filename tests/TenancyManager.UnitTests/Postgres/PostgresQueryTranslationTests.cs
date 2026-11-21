using Microsoft.EntityFrameworkCore;
using TenancyManager.Application.Common;
using TenancyManager.Infrastructure.Postgres;

namespace TenancyManager.UnitTests.Postgres;

/// <summary>
/// Translates every repository query to SQL without a database, so EF Core translation errors
/// show up locally (where Docker may be missing) instead of only in the Postgres contract tests.
/// </summary>
public sealed class PostgresQueryTranslationTests
{
    private static TenancyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>();
        PostgresOptions.Configure(options, "Host=unused;Database=unused");
        return new TenancyDbContext(options.Options, new FixedTenant());
    }

    [Fact]
    public void Property_list_query_translates_with_ordinal_ordering_and_agency_filter()
    {
        using var db = CreateContext();

        var sql = PostgresPropertyRepository.ListQuery(db).ToQueryString();

        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("COLLATE \"C\"");
        sql.ShouldContain("agency_id");
    }

    [Fact]
    public void Property_get_query_translates()
    {
        using var db = CreateContext();

        var sql = PostgresPropertyRepository.GetQuery(db, Guid.NewGuid()).ToQueryString();

        sql.ShouldContain("WHERE");
    }

    private sealed class FixedTenant : ITenantContext
    {
        public Guid AgencyId => TestPaths.Northgate;
    }
}
