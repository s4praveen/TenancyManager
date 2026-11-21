using Microsoft.EntityFrameworkCore;
using TenancyManager.Application.Properties;
using TenancyManager.Domain.Landlords;
using TenancyManager.Domain.Properties;

namespace TenancyManager.Infrastructure.Postgres;

internal sealed class PostgresPropertyRepository(TenancyDbContext db) : IPropertyRepository
{
    public async Task<IReadOnlyList<PropertySummary>> ListAsync(CancellationToken cancellationToken = default) =>
        await ListQuery(db).ToListAsync(cancellationToken);

    public Task<PropertySummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetQuery(db, id).FirstOrDefaultAsync(cancellationToken);

    // Filter and order on the entities, project last: EF Core cannot translate member access on a
    // record built with a constructor (e.g. ordering by PropertySummary.Town after the Select).
    internal static IQueryable<PropertySummary> ListQuery(TenancyDbContext db) =>
        Joined(db)
            // "C" collation = ordinal ordering, matching the Json provider (DAT-002).
            .OrderBy(x => EF.Functions.Collate(x.Property.Address.Town, "C"))
            .ThenBy(x => EF.Functions.Collate(x.Property.Address.Line1, "C"))
            .Select(x => new PropertySummary(
                x.Property.Id,
                x.Property.Address.Line1,
                x.Property.Address.Line2,
                x.Property.Address.Town,
                x.Property.Address.Postcode,
                x.Property.Jurisdiction,
                x.Property.Bedrooms,
                x.Property.LandlordId,
                x.Landlord.DisplayName));

    internal static IQueryable<PropertySummary> GetQuery(TenancyDbContext db, Guid id) =>
        Joined(db)
            .Where(x => x.Property.Id == id)
            .Select(x => new PropertySummary(
                x.Property.Id,
                x.Property.Address.Line1,
                x.Property.Address.Line2,
                x.Property.Address.Town,
                x.Property.Address.Postcode,
                x.Property.Jurisdiction,
                x.Property.Bedrooms,
                x.Property.LandlordId,
                x.Landlord.DisplayName));

    // Both sets are agency-filtered by the global query filters; no manual AgencyId check here.
    private static IQueryable<PropertyWithLandlord> Joined(TenancyDbContext db) =>
        db.Properties.AsNoTracking()
            .Join(db.Landlords, p => p.LandlordId, l => l.Id, (p, l) => new PropertyWithLandlord { Property = p, Landlord = l });

    private sealed class PropertyWithLandlord
    {
        public required Property Property { get; init; }

        public required Landlord Landlord { get; init; }
    }
}
