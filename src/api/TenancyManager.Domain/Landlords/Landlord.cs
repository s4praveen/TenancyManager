using TenancyManager.Domain.Common;

namespace TenancyManager.Domain.Landlords;

public sealed class Landlord : AgencyOwnedEntity
{
    public required string DisplayName { get; init; }

    public string? Email { get; init; }
}
