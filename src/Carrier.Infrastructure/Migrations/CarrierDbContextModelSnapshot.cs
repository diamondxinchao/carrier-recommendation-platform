using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Carrier.Infrastructure.Migrations;

[DbContext(typeof(CarrierDbContext))]
public sealed class CarrierDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0").HasAnnotation("Relational:MaxIdentifierLength", 128);
        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);
        modelBuilder.Entity("Carrier.Domain.CarrierProfile", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<DateOnly>("AvailableFrom").HasColumnType("date");
            b.Property<DateOnly>("AvailableThrough").HasColumnType("date");
            b.Property<int>("CapacityLb").HasColumnType("int");
            b.Property<int>("CompletedLoads").HasColumnType("int");
            b.Property<string>("DotNumber").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<string>("Equipment").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)");
            b.Property<double>("OnTimeRate").HasColumnType("float");
            b.Property<decimal>("RatePerMile").HasPrecision(8, 2).HasColumnType("decimal(8,2)");
            b.Property<double>("SafetyScore").HasColumnType("float");
            b.HasKey("Id");
            b.HasIndex("DotNumber").IsUnique();
            b.HasIndex("Equipment", "AvailableFrom", "CapacityLb");
            b.ToTable("Carriers");
        });
        modelBuilder.Entity("Carrier.Domain.ServiceLane", b =>
        {
            b.Property<Guid>("CarrierId").HasColumnType("uniqueidentifier");
            b.Property<string>("Origin").HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<string>("Destination").HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.HasKey("CarrierId", "Origin", "Destination");
            b.HasIndex("Origin", "Destination", "CarrierId");
            b.ToTable("ServiceLanes");
        });
        modelBuilder.Entity("Carrier.Domain.ServiceLane", b =>
            b.HasOne("Carrier.Domain.CarrierProfile", null).WithMany("Lanes").HasForeignKey("CarrierId")
                .OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("Carrier.Domain.CarrierProfile", b => b.Navigation("Lanes"));
    }
}
