namespace TenancyManager.Infrastructure;

public sealed class DataProviderOptions
{
    public const string SectionName = "DataProvider";
    public const string Json = "Json";
    public const string Postgres = "Postgres";
    public static readonly IReadOnlyList<string> Supported = [Json, Postgres];

    /// <summary>"Json" or "Postgres".</summary>
    public string Type { get; set; } = Json;

    /// <summary>Seed folder. A relative path is searched for upwards from the content root.</summary>
    public string SeedPath { get; set; } = "seed";

    /// <summary>Postgres only: apply migrations and seed an empty database at start-up.</summary>
    public bool MigrateOnStartup { get; set; } = true;
}
