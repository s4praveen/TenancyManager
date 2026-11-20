using TenancyManager.Application.Common;
using TenancyManager.Application.Properties;

namespace TenancyManager.Infrastructure.Json;

internal sealed class JsonPropertyRepository(JsonDataStore store, ITenantContext tenant) : IPropertyRepository
{
    public Task<IReadOnlyList<PropertySummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PropertySummary> result = Scoped()
            .OrderBy(p => p.Town, StringComparer.Ordinal)
            .ThenBy(p => p.Line1, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<PropertySummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Scoped().FirstOrDefault(p => p.Id == id));

    private IEnumerable<PropertySummary> Scoped()
    {
        var landlords = store.Data.Landlords
            .Where(l => l.AgencyId == tenant.AgencyId)
            .ToDictionary(l => l.Id, l => l.DisplayName);

        return store.Data.Properties
            .Where(p => p.AgencyId == tenant.AgencyId)
            .Select(p => new PropertySummary(
                p.Id,
                p.Address.Line1,
                p.Address.Line2,
                p.Address.Town,
                p.Address.Postcode,
                p.Jurisdiction,
                p.Bedrooms,
                p.LandlordId,
                landlords.GetValueOrDefault(p.LandlordId, string.Empty)));
    }
}
