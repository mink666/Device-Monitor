using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiDiskHistoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalDisk",
                table: "DeviceHistories",
                newName: "TotalDiskCKBytes");

            migrationBuilder.RenameColumn(
                name: "UsedDiskKBytes",
                table: "DeviceHistories",
                newName: "UsedDiskCKBytes");

            migrationBuilder.RenameColumn(
                name: "DiskUsagePercentage",
                table: "DeviceHistories",
                newName: "DiskCUsagePercentage");

            migrationBuilder.AddColumn<long>(
                name: "TotalDiskDKBytes",
                table: "DeviceHistories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UsedDiskDKBytes",
                table: "DeviceHistories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiskDUsagePercentage",
                table: "DeviceHistories",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalDiskEKBytes",
                table: "DeviceHistories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UsedDiskEKBytes",
                table: "DeviceHistories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiskEUsagePercentage",
                table: "DeviceHistories",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TotalDiskDKBytes", table: "DeviceHistories");
            migrationBuilder.DropColumn(name: "UsedDiskDKBytes", table: "DeviceHistories");
            migrationBuilder.DropColumn(name: "DiskDUsagePercentage", table: "DeviceHistories");
            migrationBuilder.DropColumn(name: "TotalDiskEKBytes", table: "DeviceHistories");
            migrationBuilder.DropColumn(name: "UsedDiskEKBytes", table: "DeviceHistories");
            migrationBuilder.DropColumn(name: "DiskEUsagePercentage", table: "DeviceHistories");

            // Rename the Disk C columns back to their original generic names
            migrationBuilder.RenameColumn(
                name: "TotalDiskCKBytes",
                table: "DeviceHistories",
                newName: "TotalDisk");

            migrationBuilder.RenameColumn(
                name: "UsedDiskCKBytes",
                table: "DeviceHistories",
                newName: "UsedDiskKBytes");

            migrationBuilder.RenameColumn(
                name: "DiskCUsagePercentage",
                table: "DeviceHistories",
                newName: "DiskUsagePercentage");
        }
    }
}
