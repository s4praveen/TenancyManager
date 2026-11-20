using Microsoft.EntityFrameworkCore;
using TenancyManager.Application.Agencies;
using TenancyManager.Application.Common;
using TenancyManager.Domain.Agencies;

namespace TenancyManager.Infrastructure.Postgres;

internal sealed class PostgresAgencyRepository(TenancyDbContext db, ITenantContext tenant) : IAgencyRepository
{
    public Task<Agency?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        db.Agencies.AsNoTracking().FirstOrDefaultAsync(a => a.Id == tenant.AgencyId, cancellationToken);
}
