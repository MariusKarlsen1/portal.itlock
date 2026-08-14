using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ComponentTypes",
                columns: new[] { "Id", "Navn" },
                values: new object[,]
                {
                    { 1, "Dørblad" },
                    { 2, "Karm" },
                    { 3, "Hengsler" },
                    { 4, "Terskel" },
                    { 5, "Låskasse" },
                    { 6, "Sluttstykke" },
                    { 7, "Dørvrider" },
                    { 8, "Sylinder/lås" },
                    { 9, "Panikkbeslag" },
                    { 10, "Dørpumpe" },
                    { 11, "Dørautomatikk" },
                    { 12, "Koordinering" },
                    { 13, "Kortleser" },
                    { 14, "Albuebryter" },
                    { 15, "Utspaseringsknapp" },
                    { 16, "Koblingsboks" }
                });

            migrationBuilder.InsertData(
                table: "RequirementDimensions",
                columns: new[] { "Id", "Navn", "Rekkefolge" },
                values: new object[,]
                {
                    { 1, "Dørtype", 1 },
                    { 2, "Brannklasse", 2 },
                    { 3, "Risikoklasse", 3 },
                    { 4, "Rømningskrav", 4 },
                    { 5, "Sikkerhetsklasse", 5 },
                    { 6, "Automatisk døråpner", 6 }
                });

            migrationBuilder.InsertData(
                table: "RequirementValues",
                columns: new[] { "Id", "Kode", "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[,]
                {
                    { 1, null, 1, 1, "Innerdør" },
                    { 2, null, 2, 1, "Ytterdør" },
                    { 3, null, 3, 1, "Branndør" },
                    { 4, null, 1, 2, "Ingen" },
                    { 5, null, 2, 2, "EI30" },
                    { 6, null, 3, 2, "EI60" },
                    { 7, null, 1, 3, "RKL1" },
                    { 8, null, 2, 3, "RKL2" },
                    { 9, null, 3, 3, "RKL3" },
                    { 10, null, 4, 3, "RKL4" },
                    { 11, null, 5, 3, "RKL5" },
                    { 12, null, 6, 3, "RKL6" },
                    { 13, null, 1, 4, "Rømning" },
                    { 14, null, 2, 4, "Ikke rømning" },
                    { 15, null, 3, 4, "Tilbakerømning" },
                    { 16, null, 1, 5, "RC1" },
                    { 17, null, 2, 5, "RC2" },
                    { 18, null, 3, 5, "RC3" },
                    { 19, null, 4, 5, "RC4" },
                    { 20, null, 5, 5, "RC5" },
                    { 21, null, 1, 6, "Ja" },
                    { 22, null, 2, 6, "Nei" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
