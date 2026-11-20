using TenancyManager.Domain.Agencies;

namespace TenancyManager.Application.Agencies;

public interface IAgencyRepository
{
    /// <summary>The agency identified by the current <see cref="Common.ITenantContext"/>.</summary>
    Task<Agency?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
