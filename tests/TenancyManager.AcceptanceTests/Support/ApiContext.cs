using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenancyManager.AcceptanceTests.Support;

/// <summary>Per-scenario state, injected into step classes by Reqnroll.</summary>
public sealed class ApiContext : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private TenancyApiFactory? _customFactory;

    public string Environment { get; set; } = "Testing";

    public Dictionary<string, string?> Settings { get; } = [];

    public string? User { get; set; }

    public List<string> Roles { get; } = [];

    public HttpResponseMessage? Response { get; private set; }

    public Exception? StartupError { get; private set; }

    public async Task SendAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (User is not null)
        {
            request.Headers.Add("X-Dev-User", User);
            request.Headers.Add("X-Dev-Roles", string.Join(',', Roles));
        }

        Response = await Client().SendAsync(request);
        // Buffer once: several Then steps read the same response, and the stream is single-use.
        Body = await Response.Content.ReadAsStringAsync();
    }

    public string Body { get; private set; } = string.Empty;

    public Task<T> ReadAsync<T>()
    {
        Response.ShouldNotBeNull();
        Response.StatusCode.ShouldBe(HttpStatusCode.OK, Body);
        return Task.FromResult(JsonSerializer.Deserialize<T>(Body, Json)!);
    }

    public void TryStart()
    {
        try
        {
            using var _ = Client();
        }
#pragma warning disable CA1031 // the scenario asserts on whatever start-up threw
        catch (Exception ex)
#pragma warning restore CA1031
        {
            StartupError = ex;
        }
    }

    private HttpClient Client()
    {
        if (Settings.Count == 0 && Environment == "Testing")
        {
            return SharedHost.Factory.CreateClient();
        }

        _customFactory ??= new TenancyApiFactory(Environment, Settings);
        return _customFactory.CreateClient();
    }

    public void Dispose()
    {
        Response?.Dispose();
        _customFactory?.Dispose();
    }
}
