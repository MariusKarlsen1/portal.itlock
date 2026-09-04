using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class CeGodkjenningEkstraFelter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Byggkategori",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CeGyldighetMåneder",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Risikoklasse",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Energi",
                table: "Dorer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DorIdKode",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnergiKlasse",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GyldighetMåneder",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Byggkategori",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "CeGyldighetMåneder",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "Risikoklasse",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "Energi",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "DorIdKode",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "EnergiKlasse",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "GyldighetMåneder",
                table: "CeGodkjenninger");
        }
    }
}
