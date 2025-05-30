using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthStatusAndDiskMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatteryLevel",
                table: "DeviceHistories");

            migrationBuilder.DropColumn(
                name: "TotalRamKBytes",
                table: "DeviceHistories");

            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthStatusReason",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiskUsagePercentage",
                table: "DeviceHistories",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "HealthStatusReason",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "DiskUsagePercentage",
                table: "DeviceHistories");

            migrationBuilder.AddColumn<int>(
                name: "BatteryLevel",
                table: "DeviceHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalRamKBytes",
                table: "DeviceHistories",
                type: "bigint",
                nullable: true);
        }
    }
}
