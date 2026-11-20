using TenancyManager.Application.Agencies;
using TenancyManager.Application.Common;
using TenancyManager.Domain.Agencies;

namespace TenancyManager.Infrastructure.Json;

internal sealed class JsonAgencyRepository(JsonDataStore store, ITenantContext tenant) : IAgencyRepository
{
    public Task<Agency?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Data.Agencies.FirstOrDefault(a => a.Id == tenant.AgencyId));
}
