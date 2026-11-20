using TenancyManager.Application.Properties;
using TenancyManager.Infrastructure.Json;
using TenancyManager.Infrastructure.Postgres;

namespace TenancyManager.IntegrationTests.Postgres;

[Collection(PostgresCollection.Name)]
public sealed class PostgresPropertyRepositoryTests(PostgresFixture fixture) : PropertyRepositoryContract, IDisposable
{
    private readonly List<TenancyDbContext> _contexts = [];

    protected override Task<IPropertyRepository> CreateAsync(Guid agencyId)
    {
        Skip.If(fixture.UnavailableReason is not null, fixture.UnavailableReason);
        var db = fixture.CreateContext(agencyId);
        _contexts.Add(db);
        return Task.FromResult<IPropertyRepository>(new PostgresPropertyRepository(db));
    }

    [SkippableFact]
    [Trait("Requirement", "DAT-002")]
    public async Task Seeded_postgres_returns_the_same_property_list_as_json()
    {
        var postgres = await CreateAsync(Northgate);
        var json = new JsonPropertyRepository(new JsonDataStore(Seeds.Load()), new FixedTenant(Northgate));

        (await postgres.ListAsync()).ShouldBe(await json.ListAsync());
    }

    public void Dispose()
    {
        foreach (var db in _contexts)
        {
            db.Dispose();
        }
    }
}
