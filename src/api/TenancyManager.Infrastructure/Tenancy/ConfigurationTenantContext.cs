using Microsoft.Extensions.Configuration;
using TenancyManager.Application.Common;

namespace TenancyManager.Infrastructure.Tenancy;

/// <summary>Single-agency mode: the agency comes from <c>Agency:DefaultAgencyId</c>.</summary>
public sealed class ConfigurationTenantContext(IConfiguration configuration) : ITenantContext
{
    public Guid AgencyId { get; } = Guid.TryParse(configuration["Agency:DefaultAgencyId"], out var id)
        ? id
        : throw new InvalidOperationException("Configuration value 'Agency:DefaultAgencyId' must be a GUID.");
}
