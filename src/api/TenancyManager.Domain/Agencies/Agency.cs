using TenancyManager.Domain.Common;

namespace TenancyManager.Domain.Agencies;

public sealed class Agency : Entity
{
    public required string Name { get; init; }
}
