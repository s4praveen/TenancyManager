using TenancyManager.Application.Properties;
using TenancyManager.Domain.Common;

namespace TenancyManager.IntegrationTests;

/// <summary>
/// Behaviour every <see cref="IPropertyRepository"/> must have. Each provider subclasses this,
/// which is how we keep Json and Postgres identical.
/// </summary>
public abstract class PropertyRepositoryContract
{
    protected static readonly Guid Northgate = Guid.Parse("11111111-1111-1111-1111-111111111111");
    protected static readonly Guid Southbank = Guid.Parse("22222222-2222-2222-2222-222222222222");
    protected static readonly Guid SouthbankProperty = Guid.Parse("b2000000-0000-0000-0000-000000000001");
    protected static readonly Guid CardiffProperty = Guid.Parse("b1000000-0000-0000-0000-000000000001");

    protected abstract Task<IPropertyRepository> CreateAsync(Guid agencyId);

    [SkippableFact]
    [Trait("Requirement", "PRP-001")]
    public async Task Lists_agency_properties_ordered_by_town_then_line1()
    {
        var repo = await CreateAsync(Northgate);

        var list = await repo.ListAsync();

        list.Select(p => p.Town).ShouldBe(["Cardiff", "Edinburgh", "Manchester", "York"]);
        list[0].ShouldBe(new PropertySummary(
            CardiffProperty, "14 Castle Street", null, "Cardiff", "CF10 1BU", Jurisdiction.Wales, 2,
            Guid.Parse("a1000000-0000-0000-0000-000000000001"), "Gwen Davies"));
    }

    [SkippableFact]
    [Trait("Requirement", "AGY-001")]
    public async Task List_excludes_other_agencies()
    {
        var repo = await CreateAsync(Southbank);

        var list = await repo.ListAsync();

        list.Count.ShouldBe(2);
        list.ShouldAllBe(p => p.Town == "London");
    }

    [SkippableFact]
    [Trait("Requirement", "AGY-002")]
    public async Task Get_returns_null_for_another_agencys_property()
    {
        var repo = await CreateAsync(Northgate);

        (await repo.GetAsync(SouthbankProperty)).ShouldBeNull();
    }

    [SkippableFact]
    [Trait("Requirement", "PRP-003")]
    public async Task Get_returns_own_property_and_null_for_unknown_id()
    {
        var repo = await CreateAsync(Northgate);

        (await repo.GetAsync(CardiffProperty))!.LandlordName.ShouldBe("Gwen Davies");
        (await repo.GetAsync(Guid.NewGuid())).ShouldBeNull();
    }
}
