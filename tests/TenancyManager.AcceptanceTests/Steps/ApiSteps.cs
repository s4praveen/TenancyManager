using System.Text.Json;
using Reqnroll;
using TenancyManager.AcceptanceTests.Support;

namespace TenancyManager.AcceptanceTests.Steps;

[Binding]
public sealed class ApiSteps(ApiContext ctx)
{
    // ---- Who is calling -------------------------------------------------------------------

    [Given("a caller with no credentials")]
    public void GivenAnonymous() => ctx.User = null;

    [Given("an authenticated user {string} with no roles")]
    public void GivenUserWithNoRoles(string user) => ctx.User = user;

    [Given("an authenticated user {string} with role {string}")]
    public void GivenUserWithRole(string user, string role)
    {
        ctx.User = user;
        ctx.Roles.Add(role);
    }

    // ---- Configuration --------------------------------------------------------------------

    [Given("the environment is {string}")]
    public void GivenEnvironment(string environment) => ctx.Environment = environment;

    [Given("the authentication mode is {string}")]
    public void GivenAuthMode(string mode) => ctx.Settings["Auth:Mode"] = mode;

    [Given("the data provider is configured as {string}")]
    [Given("the API is running with the {string} data provider")]
    public void GivenDataProvider(string provider)
    {
        // "Json" is the default host; only override when it differs, so the shared host is reused.
        if (provider != "Json")
        {
            ctx.Settings["DataProvider:Type"] = provider;
        }
    }

    [Given("the current agency is {string}")]
    public void GivenCurrentAgency(string agency)
    {
        var id = SeedCatalog.AgencyId(agency).ToString();
        if (id != TenancyApiFactory.Defaults["Agency:DefaultAgencyId"])
        {
            ctx.Settings["Agency:DefaultAgencyId"] = id;
        }
    }

    [Given("the store also holds properties for {string}")]
    public void GivenOtherAgencyHasProperties(string agency) =>
        SeedCatalog.PropertyIds(agency).ShouldNotBeEmpty();

    // ---- Requests -------------------------------------------------------------------------

    [When("the caller requests the property list")]
    [When("the user requests the property list")]
    public Task WhenPropertyList() => ctx.SendAsync("/api/properties");

    [When("an agent requests the property list")]
    public Task WhenAgentRequestsPropertyList()
    {
        AsAgent();
        return ctx.SendAsync("/api/properties");
    }

    [When("an agent requests a property that belongs to {string} by its id")]
    public Task WhenAgentRequestsOtherAgencyProperty(string agency)
    {
        AsAgent();
        return ctx.SendAsync($"/api/properties/{SeedCatalog.PropertyIds(agency)[0]}");
    }

    [When("the user requests a property with an id that does not exist")]
    public Task WhenUnknownProperty() => ctx.SendAsync($"/api/properties/{Guid.NewGuid()}");

    [When("the user requests their profile")]
    public Task WhenProfile() => ctx.SendAsync("/api/me");

    [When("an anonymous caller requests the health endpoint")]
    public Task WhenHealth()
    {
        ctx.User = null;
        return ctx.SendAsync("/health");
    }

    [When("the application starts")]
    public void WhenApplicationStarts() => ctx.TryStart();

    // ---- Outcomes -------------------------------------------------------------------------

    [Then("the response status is {int}")]
    public void ThenStatus(int status) => ((int)ctx.Response!.StatusCode).ShouldBe(status);

    [Then("the response contains user {string}")]
    public async Task ThenUser(string user) => (await ctx.ReadAsync<Me>()).UserName.ShouldBe(user);

    [Then("the roles include {string}")]
    public async Task ThenRole(string role) => (await ctx.ReadAsync<Me>()).Roles.ShouldContain(role);

    [Then("the agency name is {string}")]
    public async Task ThenAgency(string name) => (await ctx.ReadAsync<Me>()).Agency.Name.ShouldBe(name);

    [Then("{int} properties are returned")]
    public async Task ThenPropertyCount(int count) => (await ctx.ReadAsync<List<PropertyItem>>()).Count.ShouldBe(count);

    [Then("the first property is {string} in jurisdiction {string} owned by {string}")]
    public async Task ThenFirstProperty(string address, string jurisdiction, string landlord)
    {
        var first = (await ctx.ReadAsync<List<PropertyItem>>())[0];
        $"{first.Line1}, {first.Town}".ShouldBe(address);
        first.Jurisdiction.ShouldBe(jurisdiction);
        first.LandlordName.ShouldBe(landlord);
    }

    [Then("the jurisdictions returned are exactly {string}, {string} and {string}")]
    public async Task ThenJurisdictions(string a, string b, string c) =>
        (await ctx.ReadAsync<List<PropertyItem>>()).Select(p => p.Jurisdiction).Distinct().Order()
            .ShouldBe(new[] { a, b, c }.Order());

    [Then("every property returned belongs to {string}")]
    public async Task ThenAllBelongTo(string agency)
    {
        var own = SeedCatalog.PropertyIds(agency);
        (await ctx.ReadAsync<List<PropertyItem>>()).ShouldAllBe(p => own.Contains(p.Id));
    }

    [Then("no {string} property is returned")]
    public async Task ThenNoneFrom(string agency)
    {
        var other = SeedCatalog.PropertyIds(agency);
        (await ctx.ReadAsync<List<PropertyItem>>()).ShouldNotContain(p => other.Contains(p.Id));
    }

    [Then("the health status is {string}")]
    public async Task ThenHealthStatus(string status) =>
        (await ctx.ReadAsync<JsonElement>()).GetProperty("status").GetString().ShouldBe(status);

    [Then("the data provider is {string}")]
    public async Task ThenDataProvider(string provider) =>
        (await ctx.ReadAsync<JsonElement>()).GetProperty("dataProvider").GetString().ShouldBe(provider);

    [Then("start-up fails with an error containing {string}")]
    public void ThenStartupFails(string message)
    {
        ctx.StartupError.ShouldNotBeNull("expected the application to fail at start-up");
        AllMessages(ctx.StartupError).ShouldContain(m => m.Contains(message, StringComparison.Ordinal));
    }

    private void AsAgent()
    {
        ctx.User ??= "agent@example.test";
        if (ctx.Roles.Count == 0)
        {
            ctx.Roles.Add("Agent");
        }
    }

    private static IEnumerable<string> AllMessages(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            yield return e.Message;
            if (e is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions.SelectMany(AllMessages))
                {
                    yield return inner;
                }
            }
        }
    }

    private sealed record Me(string UserName, string DisplayName, List<string> Roles, AgencyItem Agency);

    private sealed record AgencyItem(Guid Id, string Name);

    private sealed record PropertyItem(Guid Id, string Line1, string? Line2, string Town, string Postcode,
        string Jurisdiction, int Bedrooms, Guid LandlordId, string LandlordName);
}
