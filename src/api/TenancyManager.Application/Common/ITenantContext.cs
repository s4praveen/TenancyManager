namespace TenancyManager.Application.Common;

/// <summary>
/// The agency the current request/operation acts for. Today it comes from configuration;
/// in the SaaS phase it will come from the user's token. Callers must not care which.
/// </summary>
public interface ITenantContext
{
    Guid AgencyId { get; }
}
