using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class SystemtabellGruppeOgProdusent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gruppe",
                table: "NedlastningsKategorier",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Produsent",
                table: "NedlastningsKategorier",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gruppe",
                table: "KoblingsKategorier",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Produsent",
                table: "KoblingsKategorier",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Gruppe", "Produsent" },
                values: new object[] { "Adgangskontroll", "ARX" });

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Gruppe", "Produsent" },
                values: new object[] { "Adgangskontroll", "Salto" });

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Gruppe", "Produsent" },
                values: new object[] { "Andre", null });

            migrationBuilder.UpdateData(
                table: "KoblingsKategorier",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Gruppe", "Produsent" },
                values: new object[] { "Andre", null });

            migrationBuilder.UpdateData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Gruppe", "Produsent" },
                values: new object[] { "Adgangskontroll", "Salto" });

            migrationBuilder.UpdateData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Gruppe", "Produsent" },
                values: new object[] { "Adgangskontroll", "Salto" });

            migrationBuilder.UpdateData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Gruppe", "Produsent" },
                values: new object[] { "Adgangskontroll", "ARX" });

            migrationBuilder.UpdateData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Gruppe", "Produsent" },
                values: new object[] { "Låssystem", "iLOQ" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gruppe",
                table: "NedlastningsKategorier");

            migrationBuilder.DropColumn(
                name: "Produsent",
                table: "NedlastningsKategorier");

            migrationBuilder.DropColumn(
                name: "Gruppe",
                table: "KoblingsKategorier");

            migrationBuilder.DropColumn(
                name: "Produsent",
                table: "KoblingsKategorier");
        }
    }
}
