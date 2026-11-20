using Reqnroll;

namespace TenancyManager.AcceptanceTests.Support;

/// <summary>One in-memory API for every scenario that uses the default settings.</summary>
[Binding]
public sealed class SharedHost
{
    private static readonly Lazy<TenancyApiFactory> LazyFactory =
        new(() => new TenancyApiFactory("Testing", new Dictionary<string, string?>()));

    public static TenancyApiFactory Factory => LazyFactory.Value;

    [AfterTestRun]
    public static async Task DisposeAsync()
    {
        if (LazyFactory.IsValueCreated)
        {
            await LazyFactory.Value.DisposeAsync();
        }
    }
}
