using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKoblingsKategoriDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Beskrivelse",
                table: "KoblingsKategorier",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Farge",
                table: "KoblingsKategorier",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Ikon",
                table: "KoblingsKategorier",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Beskrivelse", "Farge", "Ikon" },
                values: new object[] { "Koblingsskjema og dokumentasjon for ARX.", "oransje", "lock" });

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Beskrivelse", "Farge", "Ikon" },
                values: new object[] { "Koblingsskjema og dokumentasjon for Salto.", "gronn", "door" });

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Beskrivelse", "Farge", "Ikon" },
                values: new object[] { "Andre systemer og koblingsløsninger.", "gul", "settings" });

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Beskrivelse", "Farge", "Ikon" },
                values: new object[] { "Prinsippskisser og generelle koblingsskjema.", "rosa", "file-text" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Beskrivelse",
                table: "KoblingsKategorier");

            migrationBuilder.DropColumn(
                name: "Farge",
                table: "KoblingsKategorier");

            migrationBuilder.DropColumn(
                name: "Ikon",
                table: "KoblingsKategorier");
        }
    }
}
