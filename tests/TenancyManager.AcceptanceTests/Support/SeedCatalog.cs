using TenancyManager.Infrastructure.Seed;

namespace TenancyManager.AcceptanceTests.Support;

/// <summary>Lets steps refer to seed records by name, so feature files stay readable.</summary>
public static class SeedCatalog
{
    public static SeedData Data { get; } = SeedReader.Read(SeedReader.ResolveRoot("seed", AppContext.BaseDirectory));

    public static Guid AgencyId(string name) =>
        Data.Agencies.SingleOrDefault(a => a.Name == name)?.Id
        ?? throw new ArgumentException($"No seed agency named '{name}'.", nameof(name));

    public static IReadOnlyList<Guid> PropertyIds(string agencyName)
    {
        var id = AgencyId(agencyName);
        return Data.Properties.Where(p => p.AgencyId == id).Select(p => p.Id).ToList();
    }
}
