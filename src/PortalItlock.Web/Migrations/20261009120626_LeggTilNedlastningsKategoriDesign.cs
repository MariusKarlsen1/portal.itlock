using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilNedlastningsKategoriDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Beskrivelse",
                table: "NedlastningsKategorier",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Farge",
                table: "NedlastningsKategorier",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Beskrivelse", "Farge" },
                values: new object[] { "Oppsett, dørmiljø, kortlesere og rettigheter.", "brun" });

            migrationBuilder.UpdateData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Beskrivelse", "Farge" },
                values: new object[] { "Skybasert adgangskontroll og brukere.", "bla" });

            migrationBuilder.UpdateData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Beskrivelse", "Farge" },
                values: new object[] { "Konfigurasjon av sentraler, dørkort og I/O.", "gronn" });

            migrationBuilder.UpdateData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Beskrivelse", "Farge" },
                values: new object[] { "Digital sylinder, nøkler og administrasjon.", "gra" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Beskrivelse",
                table: "NedlastningsKategorier");

            migrationBuilder.DropColumn(
                name: "Farge",
                table: "NedlastningsKategorier");
        }
    }
}
