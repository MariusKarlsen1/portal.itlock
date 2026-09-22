using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilArxOgIloqNedlastningskategorier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "NedlastningsKategorier",
                columns: new[] { "Id", "Navn", "Rekkefolge" },
                values: new object[,]
                {
                    { 3, "ARX", 3 },
                    { 4, "iLOQ", 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "NedlastningsKategorier",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
