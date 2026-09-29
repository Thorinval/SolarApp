using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtmoceSolarApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceSerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlarmId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlarmName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlarmLevel = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AlarmCause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlarmRepairSuggestion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlarmOccurTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Resolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuildState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountryGec = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountryStanag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConstructionMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SolarCapacity = table.Column<double>(type: "float", nullable: false),
                    BatteryCapacity = table.Column<double>(type: "float", nullable: false),
                    MICapacity = table.Column<double>(type: "float", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerMail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GridTiedTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    DeviceSerialNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceMode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Capacity = table.Column<double>(type: "float", nullable: false),
                    MaxChargepower = table.Column<double>(type: "float", nullable: true),
                    MaxDischargepower = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastReportedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteDataSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    SolarGenerationPower = table.Column<int>(type: "int", nullable: false),
                    SolarReactivePower = table.Column<int>(type: "int", nullable: false),
                    DailySolarGeneration = table.Column<double>(type: "float", nullable: false),
                    MonthlySolarGeneration = table.Column<double>(type: "float", nullable: false),
                    YearlySolarGeneration = table.Column<double>(type: "float", nullable: false),
                    LifetimeSolarGeneration = table.Column<double>(type: "float", nullable: false),
                    GridPower = table.Column<int>(type: "int", nullable: false),
                    DailyGridExport = table.Column<double>(type: "float", nullable: false),
                    DailyGridImport = table.Column<double>(type: "float", nullable: false),
                    MonthlyGridExport = table.Column<double>(type: "float", nullable: false),
                    MonthlyGridImport = table.Column<double>(type: "float", nullable: false),
                    YearlyGridExport = table.Column<double>(type: "float", nullable: false),
                    YearlyGridImport = table.Column<double>(type: "float", nullable: false),
                    LifetimeGridExport = table.Column<double>(type: "float", nullable: false),
                    LifetimeGridImport = table.Column<double>(type: "float", nullable: false),
                    ConsumptionPower = table.Column<int>(type: "int", nullable: false),
                    DailyConsumption = table.Column<double>(type: "float", nullable: false),
                    MonthlyConsumption = table.Column<double>(type: "float", nullable: false),
                    LifetimeConsumption = table.Column<double>(type: "float", nullable: false),
                    BatteryPower = table.Column<int>(type: "int", nullable: false),
                    BatterySOC = table.Column<double>(type: "float", nullable: false),
                    BatteryStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BatteryMode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DailyBatteryCharging = table.Column<double>(type: "float", nullable: false),
                    DailyBatteryDischarge = table.Column<double>(type: "float", nullable: false),
                    YearlyBatteryCharging = table.Column<double>(type: "float", nullable: false),
                    YearlyBatteryDischarge = table.Column<double>(type: "float", nullable: false),
                    TotalCo2Reduced = table.Column<double>(type: "float", nullable: false),
                    TotalTreesPlanted = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteDataSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteDataSnapshots_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceDataSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    CurrentPower = table.Column<int>(type: "int", nullable: false),
                    DailyGeneration = table.Column<double>(type: "float", nullable: false),
                    MonthlyGeneration = table.Column<double>(type: "float", nullable: false),
                    YearlyGeneration = table.Column<double>(type: "float", nullable: false),
                    LifetimeGeneration = table.Column<double>(type: "float", nullable: false),
                    SOC = table.Column<int>(type: "int", nullable: true),
                    DailyCharging = table.Column<double>(type: "float", nullable: true),
                    DailyDischarge = table.Column<double>(type: "float", nullable: true),
                    MonthlyCharging = table.Column<double>(type: "float", nullable: true),
                    MonthlyDischarge = table.Column<double>(type: "float", nullable: true),
                    GridVoltage = table.Column<int>(type: "int", nullable: true),
                    GridVoltageA = table.Column<int>(type: "int", nullable: true),
                    GridVoltageB = table.Column<int>(type: "int", nullable: true),
                    GridVoltageC = table.Column<int>(type: "int", nullable: true),
                    SnapshotTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceDataSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceDataSnapshots_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAlerts_AlarmLevel",
                table: "DeviceAlerts",
                column: "AlarmLevel");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAlerts_SiteId_AlarmOccurTime",
                table: "DeviceAlerts",
                columns: new[] { "SiteId", "AlarmOccurTime" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceDataSnapshots_DeviceId_SnapshotTime",
                table: "DeviceDataSnapshots",
                columns: new[] { "DeviceId", "SnapshotTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceSerialNumber",
                table: "Devices",
                column: "DeviceSerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_SiteId",
                table: "Devices",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteDataSnapshots_SiteId_SnapshotTime",
                table: "SiteDataSnapshots",
                columns: new[] { "SiteId", "SnapshotTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_SiteId",
                table: "Sites",
                column: "SiteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiTokens");

            migrationBuilder.DropTable(
                name: "DeviceAlerts");

            migrationBuilder.DropTable(
                name: "DeviceDataSnapshots");

            migrationBuilder.DropTable(
                name: "SiteDataSnapshots");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
