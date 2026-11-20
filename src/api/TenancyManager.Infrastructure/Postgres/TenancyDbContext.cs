using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TenancyManager.Application.Common;
using TenancyManager.Domain.Agencies;
using TenancyManager.Domain.Common;
using TenancyManager.Domain.Landlords;
using TenancyManager.Domain.Properties;

namespace TenancyManager.Infrastructure.Postgres;

public sealed class TenancyDbContext(DbContextOptions<TenancyDbContext> options, ITenantContext tenant)
    : DbContext(options)
{
    /// <summary>Referenced by the global query filters; EF Core re-evaluates it per query.</summary>
    internal Guid CurrentAgencyId => tenant.AgencyId;

    public DbSet<Agency> Agencies => Set<Agency>();

    public DbSet<Landlord> Landlords => Set<Landlord>();

    public DbSet<Property> Properties => Set<Property>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agency>(e =>
        {
            e.ToTable("agencies");
            e.Property(a => a.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Landlord>(e =>
        {
            e.ToTable("landlords");
            e.Property(l => l.DisplayName).HasMaxLength(200);
            e.Property(l => l.Email).HasMaxLength(320);
            e.HasIndex(l => l.AgencyId);
        });

        modelBuilder.Entity<Property>(e =>
        {
            e.ToTable("properties");
            e.ComplexProperty(p => p.Address, a =>
            {
                a.Property(x => x.Line1).HasMaxLength(200);
                a.Property(x => x.Line2).HasMaxLength(200);
                a.Property(x => x.Town).HasMaxLength(100);
                a.Property(x => x.Postcode).HasMaxLength(10);
            });
            e.Property(p => p.Jurisdiction).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Landlord>().WithMany().HasForeignKey(p => p.LandlordId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => p.AgencyId);
        });

        ApplyAgencyScoping(modelBuilder);
    }

    /// <summary>
    /// Every <see cref="AgencyOwnedEntity"/> gets an AgencyId FK and a query filter. Done by
    /// convention so a new entity cannot be forgotten (a unit test asserts this).
    /// </summary>
    private void ApplyAgencyScoping(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(AgencyOwnedEntity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasOne(typeof(Agency)).WithMany().HasForeignKey(nameof(AgencyOwnedEntity.AgencyId))
                .OnDelete(DeleteBehavior.Restrict);

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(parameter, nameof(AgencyOwnedEntity.AgencyId)),
                    Expression.Property(Expression.Constant(this), nameof(CurrentAgencyId))),
                parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
