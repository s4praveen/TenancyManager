using Microsoft.EntityFrameworkCore;
using TenancyManager.Application.Common;
using TenancyManager.Domain.Common;
using TenancyManager.Infrastructure.Postgres;

namespace TenancyManager.UnitTests.Postgres;

/// <summary>Guards the "never filter by hand" rule: every agency-owned entity is mapped and filtered.</summary>
public sealed class AgencyScopingConventionTests
{
    private static TenancyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>();
        PostgresOptions.Configure(options, "Host=unused;Database=unused");
        return new TenancyDbContext(options.Options, new FixedTenant());
    }

    [Fact]
    public void Every_agency_owned_domain_type_is_mapped()
    {
        using var db = CreateContext();
        var mapped = db.Model.GetEntityTypes().Select(t => t.ClrType).ToHashSet();

        var agencyOwned = typeof(AgencyOwnedEntity).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(AgencyOwnedEntity)));

        agencyOwned.ShouldAllBe(t => mapped.Contains(t));
    }

    [Fact]
    public void Every_agency_owned_entity_has_a_query_filter()
    {
        using var db = CreateContext();

        var unfiltered = db.Model.GetEntityTypes()
            .Where(t => typeof(AgencyOwnedEntity).IsAssignableFrom(t.ClrType))
            .Where(t => !t.GetDeclaredQueryFilters().Any())
            .Select(t => t.ClrType.Name)
            .ToList();

        unfiltered.ShouldBeEmpty();
    }

    private sealed class FixedTenant : ITenantContext
    {
        public Guid AgencyId => TestPaths.Northgate;
    }
}
