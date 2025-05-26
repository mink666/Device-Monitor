using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddSysDescrAndRamToHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawSystemDescription",
                table: "DeviceHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalRamKBytes",
                table: "DeviceHistories",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawSystemDescription",
                table: "DeviceHistories");

            migrationBuilder.DropColumn(
                name: "TotalRamKBytes",
                table: "DeviceHistories");
        }
    }
}
