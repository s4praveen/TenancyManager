namespace TenancyManager.Application.Properties;

/// <summary>
/// Agency-scoped property reads. Implemented by both the Json and Postgres providers and
/// verified by the shared contract tests.
/// </summary>
public interface IPropertyRepository
{
    /// <summary>Current agency's properties ordered by town, then first address line (PRP-001).</summary>
    Task<IReadOnlyList<PropertySummary>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns null for unknown ids and for other agencies' properties (AGY-002).</summary>
    Task<PropertySummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
