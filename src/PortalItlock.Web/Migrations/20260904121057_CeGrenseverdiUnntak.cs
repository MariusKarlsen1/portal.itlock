using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class CeGrenseverdiUnntak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ApningskraftUnntatt",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ApningstidUnntatt",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LukketidHoyUnntatt",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LukketidLavUnntatt",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApningskraftUnntatt",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "ApningstidUnntatt",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "LukketidHoyUnntatt",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "LukketidLavUnntatt",
                table: "CeGodkjenninger");
        }
    }
}
