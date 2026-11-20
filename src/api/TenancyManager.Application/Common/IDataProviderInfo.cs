namespace TenancyManager.Application.Common;

public interface IDataProviderInfo
{
    /// <summary>"Json" or "Postgres".</summary>
    string Name { get; }
}
