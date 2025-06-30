using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoginWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddWarningTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WarningStateTimestamp",
                table: "Devices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarningStateTimestamp",
                table: "Devices");
        }
    }
}
