using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtmoceSolarApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyEnergyRecordsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyEnergyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ProducedKwh = table.Column<double>(type: "float", nullable: false),
                    ConsumedKwh = table.Column<double>(type: "float", nullable: false),
                    FromGridKwh = table.Column<double>(type: "float", nullable: false),
                    ToGridKwh = table.Column<double>(type: "float", nullable: false),
                    ChargedKwh = table.Column<double>(type: "float", nullable: false),
                    DischargedKwh = table.Column<double>(type: "float", nullable: false),
                    SocMax = table.Column<double>(type: "float", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyEnergyRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyEnergyRecords_RecordDate",
                table: "DailyEnergyRecords",
                column: "RecordDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyEnergyRecords");
        }
    }
}
