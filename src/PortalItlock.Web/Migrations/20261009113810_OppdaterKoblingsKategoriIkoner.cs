using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class OppdaterKoblingsKategoriIkoner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 1,
                column: "Ikon",
                value: "krets-brikke");

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 2,
                column: "Ikon",
                value: "krets-modul");

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 3,
                column: "Ikon",
                value: "krets-bryter");

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 4,
                column: "Ikon",
                value: "krets-dokument");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 1,
                column: "Ikon",
                value: "lock");

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 2,
                column: "Ikon",
                value: "door");

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 3,
                column: "Ikon",
                value: "settings");

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 4,
                column: "Ikon",
                value: "file-text");
        }
    }
}
