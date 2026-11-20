using TenancyManager.Domain.Common;
using TenancyManager.Infrastructure.Seed;

namespace TenancyManager.UnitTests.Seed;

public sealed class SeedReaderTests
{
    [Fact]
    public void Reads_both_agencies_and_assigns_folder_agency_to_records()
    {
        var seed = SeedReader.Read(TestPaths.SeedRoot);

        seed.Agencies.Select(a => a.Name).ShouldBe(["Northgate Lettings", "Southbank Homes"], ignoreOrder: true);
        seed.Properties.Count(p => p.AgencyId == TestPaths.Northgate).ShouldBe(4);
        seed.Properties.Count(p => p.AgencyId == TestPaths.Southbank).ShouldBe(2);
        seed.Landlords.ShouldAllBe(l => l.AgencyId != Guid.Empty);
    }

    [Fact]
    public void Northgate_covers_all_three_jurisdictions()
    {
        var seed = SeedReader.Read(TestPaths.SeedRoot);

        seed.Properties.Where(p => p.AgencyId == TestPaths.Northgate)
            .Select(p => p.Jurisdiction).Distinct()
            .ShouldBe([Jurisdiction.England, Jurisdiction.Wales, Jurisdiction.Scotland], ignoreOrder: true);
    }

    [Fact]
    public void Rejects_property_whose_landlord_is_in_another_agency()
    {
        var root = Directory.CreateTempSubdirectory("tm-seed-").FullName;
        try
        {
            var dir = Directory.CreateDirectory(Path.Combine(root, "agencies", "bad")).FullName;
            File.WriteAllText(Path.Combine(dir, "agency.json"), """{ "id": "33333333-3333-3333-3333-333333333333", "name": "Bad" }""");
            File.WriteAllText(Path.Combine(dir, "landlords.json"), "[]");
            File.WriteAllText(Path.Combine(dir, "properties.json"), """
                [{ "id": "c0000000-0000-0000-0000-000000000001",
                   "address": { "line1": "1 High St", "town": "Bath", "postcode": "BA1 1AA" },
                   "jurisdiction": "England", "bedrooms": 1,
                   "landlordId": "a1000000-0000-0000-0000-000000000001" }]
                """);

            Should.Throw<InvalidDataException>(() => SeedReader.Read(root))
                .Message.ShouldContain("not in the same agency");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveRoot_throws_a_clear_error_when_folder_is_missing()
    {
        Should.Throw<DirectoryNotFoundException>(() => SeedReader.ResolveRoot("no-such-seed-folder", Path.GetTempPath()))
            .Message.ShouldContain("no-such-seed-folder");
    }
}
