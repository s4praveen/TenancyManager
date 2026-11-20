using TenancyManager.Domain.Agencies;
using TenancyManager.Domain.Landlords;
using TenancyManager.Domain.Properties;

namespace TenancyManager.Infrastructure.Seed;

public sealed record SeedData(
    IReadOnlyList<Agency> Agencies,
    IReadOnlyList<Landlord> Landlords,
    IReadOnlyList<Property> Properties);
