using TenancyManager.Domain.Common;

namespace TenancyManager.Application.Properties;

public sealed record PropertySummary(
    Guid Id,
    string Line1,
    string? Line2,
    string Town,
    string Postcode,
    Jurisdiction Jurisdiction,
    int Bedrooms,
    Guid LandlordId,
    string LandlordName);
