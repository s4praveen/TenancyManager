using System.Text.Json;
using System.Text.Json.Serialization;
using TenancyManager.Domain.Agencies;
using TenancyManager.Domain.Landlords;
using TenancyManager.Domain.Properties;

namespace TenancyManager.Infrastructure.Seed;

/// <summary>
/// Reads <c>seed/agencies/{slug}/{agency,landlords,properties}.json</c>. This is the only source of
/// fixture data for both providers: Json serves it directly, Postgres inserts it into an empty database.
/// </summary>
public static class SeedReader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static SeedData Read(string seedRoot)
    {
        var agenciesDir = Path.Combine(seedRoot, "agencies");
        if (!Directory.Exists(agenciesDir))
        {
            throw new DirectoryNotFoundException($"Seed folder '{agenciesDir}' was not found.");
        }

        var agencies = new List<Agency>();
        var landlords = new List<Landlord>();
        var properties = new List<Property>();

        foreach (var dir in Directory.GetDirectories(agenciesDir).Order(StringComparer.Ordinal))
        {
            var agency = ReadFile<Agency>(Path.Combine(dir, "agency.json"))
                ?? throw new InvalidDataException($"Agency file in '{dir}' is missing or empty.");
            agencies.Add(agency);

            // Records take the agency of their folder, so seed files never repeat agencyId.
            landlords.AddRange((ReadFile<List<Landlord>>(Path.Combine(dir, "landlords.json")) ?? [])
                .Select(l => new Landlord { Id = l.Id, AgencyId = agency.Id, DisplayName = l.DisplayName, Email = l.Email }));
            properties.AddRange((ReadFile<List<Property>>(Path.Combine(dir, "properties.json")) ?? [])
                .Select(p => new Property
                {
                    Id = p.Id,
                    AgencyId = agency.Id,
                    Address = p.Address,
                    Jurisdiction = p.Jurisdiction,
                    Bedrooms = p.Bedrooms,
                    LandlordId = p.LandlordId,
                }));
        }

        Validate(landlords, properties);
        return new SeedData(agencies, landlords, properties);
    }

    /// <summary>Resolves a possibly relative seed path by searching upwards from <paramref name="startDirectory"/>.</summary>
    public static string ResolveRoot(string seedPath, string startDirectory)
    {
        if (Path.IsPathRooted(seedPath))
        {
            return seedPath;
        }

        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, seedPath);
            if (Directory.Exists(Path.Combine(candidate, "agencies")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not find seed folder '{seedPath}' (containing 'agencies') in '{startDirectory}' or any parent folder.");
    }

    private static T? ReadFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options);
    }

    private static void Validate(List<Landlord> landlords, List<Property> properties)
    {
        var landlordAgency = landlords.ToDictionary(l => l.Id, l => l.AgencyId);
        foreach (var p in properties)
        {
            if (!landlordAgency.TryGetValue(p.LandlordId, out var agencyId) || agencyId != p.AgencyId)
            {
                throw new InvalidDataException(
                    $"Seed property {p.Id} references landlord {p.LandlordId}, which is not in the same agency.");
            }

            if (!Enum.IsDefined(p.Jurisdiction))
            {
                throw new InvalidDataException($"Seed property {p.Id} has no valid jurisdiction.");
            }
        }
    }
}
