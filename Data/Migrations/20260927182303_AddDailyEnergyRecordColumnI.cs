using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyEnergyRecordColumnI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ColumnIValue",
                table: "DailyEnergyRecords",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnIValue",
                table: "DailyEnergyRecords");
        }
    }
}

