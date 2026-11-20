using TenancyManager.Domain.Common;

namespace TenancyManager.Domain.Properties;

public sealed class Property : AgencyOwnedEntity
{
    public required Address Address { get; init; }

    public required Jurisdiction Jurisdiction { get; init; }

    public Guid LandlordId { get; init; }

    public int Bedrooms { get; init; }
}
