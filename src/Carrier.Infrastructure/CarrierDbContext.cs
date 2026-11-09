using Carrier.Domain;
using Microsoft.EntityFrameworkCore;

namespace Carrier.Infrastructure;

public sealed class CarrierDbContext(DbContextOptions<CarrierDbContext> options) : DbContext(options)
{
    public DbSet<CarrierProfile> Carriers => Set<CarrierProfile>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        var c = model.Entity<CarrierProfile>();
        c.HasKey(x => x.Id);
        c.Property(x => x.Name).HasMaxLength(120).IsRequired();
        c.Property(x => x.DotNumber).HasMaxLength(20).IsRequired();
        c.HasIndex(x => x.DotNumber).IsUnique();
        c.Property(x => x.RatePerMile).HasPrecision(8, 2);
        c.Property(x => x.Equipment).HasConversion<string>().HasMaxLength(20);
        c.HasIndex(x => new { x.Equipment, x.AvailableFrom, x.CapacityLb });
        c.HasMany(x => x.Lanes).WithOne().HasForeignKey(x => x.CarrierId).OnDelete(DeleteBehavior.Cascade);
        var l = model.Entity<ServiceLane>();
        l.ToTable("ServiceLanes");
        l.HasKey(x => new { x.CarrierId, x.Origin, x.Destination });
        l.Property(x => x.Origin).HasMaxLength(80);
        l.Property(x => x.Destination).HasMaxLength(80);
        l.HasIndex(x => new { x.Origin, x.Destination, x.CarrierId });
    }
}
