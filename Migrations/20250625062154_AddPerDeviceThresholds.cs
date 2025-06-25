using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddPerDeviceThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CpuWarningThreshold",
                table: "Devices",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiskWarningThreshold",
                table: "Devices",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RamWarningThreshold",
                table: "Devices",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpuWarningThreshold",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "DiskWarningThreshold",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RamWarningThreshold",
                table: "Devices");
        }
    }
}
