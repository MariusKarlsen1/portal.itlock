using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilInfoFelterPaTilbud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GyldigTil",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KontaktpersonEpost",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KontaktpersonNavn",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KontaktpersonTelefon",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Leveringstid",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GyldigTil",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "KontaktpersonEpost",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "KontaktpersonNavn",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "KontaktpersonTelefon",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "Leveringstid",
                table: "Tilbud");
        }
    }
}
