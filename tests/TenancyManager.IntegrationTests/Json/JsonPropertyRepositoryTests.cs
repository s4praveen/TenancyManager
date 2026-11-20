using TenancyManager.Application.Properties;
using TenancyManager.Infrastructure.Json;

namespace TenancyManager.IntegrationTests.Json;

public sealed class JsonPropertyRepositoryTests : PropertyRepositoryContract
{
    private static readonly JsonDataStore Store = new(Seeds.Load());

    protected override Task<IPropertyRepository> CreateAsync(Guid agencyId) =>
        Task.FromResult<IPropertyRepository>(new JsonPropertyRepository(Store, new FixedTenant(agencyId)));
}
