using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalRamandDisk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalRamKBytes",
                table: "DeviceHistories",
                newName: "TotalRam");

            migrationBuilder.RenameColumn(
                name: "TotalDiskKBytes",
                table: "DeviceHistories",
                newName: "TotalDisk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalRam",
                table: "DeviceHistories",
                newName: "TotalRamKBytes");

            migrationBuilder.RenameColumn(
                name: "TotalDisk",
                table: "DeviceHistories",
                newName: "TotalDiskKBytes");
        }
    }
}
