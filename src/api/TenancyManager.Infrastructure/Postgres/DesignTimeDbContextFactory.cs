using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TenancyManager.Application.Common;

namespace TenancyManager.Infrastructure.Postgres;

/// <summary>Lets <c>dotnet ef</c> build the model without starting the API or reaching a database.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    public TenancyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>();
        PostgresOptions.Configure(options, "Host=localhost;Database=tenancymanager;Username=postgres;Password=design-time");
        return new TenancyDbContext(options.Options, new DesignTimeTenant());
    }

    private sealed class DesignTimeTenant : ITenantContext
    {
        public Guid AgencyId => Guid.Empty;
    }
}
