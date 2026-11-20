using TenancyManager.Application.Common;
using TenancyManager.Infrastructure.Seed;

namespace TenancyManager.IntegrationTests;

internal sealed class FixedTenant(Guid agencyId) : ITenantContext
{
    public Guid AgencyId { get; } = agencyId;
}

internal static class Seeds
{
    public static SeedData Load() => SeedReader.Read(SeedReader.ResolveRoot("seed", AppContext.BaseDirectory));
}
