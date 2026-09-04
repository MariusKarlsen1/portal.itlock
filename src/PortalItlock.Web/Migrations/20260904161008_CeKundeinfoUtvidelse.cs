using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class CeKundeinfoUtvidelse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Arbeidsordre",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Serviceadresse",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Serviceavtale",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Arbeidsordre",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Serviceadresse",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Serviceavtale",
                table: "CeGodkjenninger");
        }
    }
}
