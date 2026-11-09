using Carrier.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Carrier.Infrastructure.Migrations;

[DbContext(typeof(CarrierDbContext))]
[Migration("20261109000100_InitialSchema")]
public sealed partial class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("Carriers", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
            DotNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
            Equipment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
            CapacityLb = table.Column<int>(type: "int", nullable: false),
            RatePerMile = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
            OnTimeRate = table.Column<double>(type: "float", nullable: false),
            SafetyScore = table.Column<double>(type: "float", nullable: false),
            CompletedLoads = table.Column<int>(type: "int", nullable: false),
            AvailableFrom = table.Column<DateOnly>(type: "date", nullable: false),
            AvailableThrough = table.Column<DateOnly>(type: "date", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Carriers", x => x.Id));
        migrationBuilder.CreateTable("ServiceLanes", table => new
        {
            CarrierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Origin = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            Destination = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ServiceLanes", x => new { x.CarrierId, x.Origin, x.Destination });
            table.ForeignKey("FK_ServiceLanes_Carriers_CarrierId", x => x.CarrierId, "Carriers", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_Carriers_DotNumber", "Carriers", "DotNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Carriers_Equipment_AvailableFrom_CapacityLb", "Carriers", new[] { "Equipment", "AvailableFrom", "CapacityLb" });
        migrationBuilder.CreateIndex("IX_ServiceLanes_Origin_Destination_CarrierId", "ServiceLanes", new[] { "Origin", "Destination", "CarrierId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ServiceLanes");
        migrationBuilder.DropTable("Carriers");
    }
}
