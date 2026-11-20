using TenancyManager.Infrastructure.Seed;

namespace TenancyManager.UnitTests;

internal static class TestPaths
{
    public static string SeedRoot { get; } = SeedReader.ResolveRoot("seed", AppContext.BaseDirectory);

    public static readonly Guid Northgate = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Southbank = Guid.Parse("22222222-2222-2222-2222-222222222222");
}
