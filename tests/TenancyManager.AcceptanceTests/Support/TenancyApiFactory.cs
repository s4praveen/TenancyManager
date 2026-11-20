using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TenancyManager.AcceptanceTests.Support;

/// <summary>
/// Hosts the real API in memory. Defaults: Json provider, Dev auth, "Testing" environment.
/// Settings use UseSetting so Program.cs sees them before Build().
/// </summary>
public sealed class TenancyApiFactory(string environment, IReadOnlyDictionary<string, string?> settings)
    : WebApplicationFactory<Program>
{
    public static readonly IReadOnlyDictionary<string, string?> Defaults = new Dictionary<string, string?>
    {
        ["Auth:Mode"] = "Dev",
        ["DataProvider:Type"] = "Json",
        ["Agency:DefaultAgencyId"] = SeedCatalog.AgencyId("Northgate Lettings").ToString(),
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        foreach (var (key, value) in Defaults.Concat(settings).GroupBy(kv => kv.Key).Select(g => g.Last()))
        {
            builder.UseSetting(key, value);
        }
    }
}
