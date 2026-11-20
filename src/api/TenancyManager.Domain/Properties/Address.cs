namespace TenancyManager.Domain.Properties;

public sealed record Address
{
    public required string Line1 { get; init; }

    public string? Line2 { get; init; }

    public required string Town { get; init; }

    public required string Postcode { get; init; }

    public override string ToString() => $"{Line1}, {Town}";
}
