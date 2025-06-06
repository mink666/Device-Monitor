using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginWeb.Migrations
{
    /// <inheritdoc />
    public partial class StoreUsedRamDiskKB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UsedDiskKBytes",
                table: "DeviceHistories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UsedRamKBytes",
                table: "DeviceHistories",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedDiskKBytes",
                table: "DeviceHistories");

            migrationBuilder.DropColumn(
                name: "UsedRamKBytes",
                table: "DeviceHistories");
        }
    }
}
