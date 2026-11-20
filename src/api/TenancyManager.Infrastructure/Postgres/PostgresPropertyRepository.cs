using Microsoft.EntityFrameworkCore;
using TenancyManager.Application.Properties;

namespace TenancyManager.Infrastructure.Postgres;

internal sealed class PostgresPropertyRepository(TenancyDbContext db) : IPropertyRepository
{
    public async Task<IReadOnlyList<PropertySummary>> ListAsync(CancellationToken cancellationToken = default) =>
        await Query()
            // "C" collation = ordinal ordering, matching the Json provider (DAT-002).
            .OrderBy(p => EF.Functions.Collate(p.Town, "C"))
            .ThenBy(p => EF.Functions.Collate(p.Line1, "C"))
            .ToListAsync(cancellationToken);

    public Task<PropertySummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    // Both sets are agency-filtered by the global query filter; no manual AgencyId check here.
    private IQueryable<PropertySummary> Query() =>
        from p in db.Properties.AsNoTracking()
        join l in db.Landlords on p.LandlordId equals l.Id
        select new PropertySummary(
            p.Id,
            p.Address.Line1,
            p.Address.Line2,
            p.Address.Town,
            p.Address.Postcode,
            p.Jurisdiction,
            p.Bedrooms,
            p.LandlordId,
            l.DisplayName);
}
