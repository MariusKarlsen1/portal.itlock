using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilAutomatiskSkyvedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // INSERT OR IGNORE (ikke InsertData, som genererer en vanlig INSERT) -
            // gjør denne migrasjonen trygg å kjøre på nytt selv om raden allerede
            // finnes i en gitt leietakerdatabase (f.eks. etter et avbrutt forsøk),
            // i stedet for å krasje hele appen på "UNIQUE constraint failed" slik
            // det gjorde i produksjon 2026-10-08.
            migrationBuilder.Sql(
                """
                INSERT OR IGNORE INTO RequirementValues (Id, Kode, Rekkefolge, RequirementDimensionId, Verdi)
                VALUES (32, NULL, 4, 6, 'Automatisk skyvedør');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 32);
        }
    }
}
