using TenancyManager.Infrastructure.Seed;

namespace TenancyManager.Infrastructure.Json;

/// <summary>In-memory store loaded once from the seed files. Read-only in Phase 0.</summary>
public sealed class JsonDataStore(SeedData data)
{
    public SeedData Data { get; } = data;
}
