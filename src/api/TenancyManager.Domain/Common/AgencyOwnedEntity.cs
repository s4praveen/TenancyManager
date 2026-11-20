namespace TenancyManager.Domain.Common;

/// <summary>
/// Base for every record that belongs to a letting agency. Isolation is enforced centrally
/// (EF Core query filters / tenant-aware Json repositories) — never filter by hand.
/// </summary>
public abstract class AgencyOwnedEntity : Entity
{
    public Guid AgencyId { get; init; }
}
