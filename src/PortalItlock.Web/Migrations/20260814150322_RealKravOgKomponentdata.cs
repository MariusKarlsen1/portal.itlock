using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class RealKravOgKomponentdata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Navn",
                value: "Låskasse 1");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Navn",
                value: "Sluttstykke 1");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Navn",
                value: "Stolpe 1");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Navn",
                value: "Sylinder 1 utv");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Navn",
                value: "Sylinder 1 innv.");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Navn",
                value: "Sylinder utstyr");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Navn",
                value: "Sylinder utstyr 2");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "Navn",
                value: "Sylinder utstyr 3");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 9,
                column: "Navn",
                value: "Håndtak");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Navn",
                value: "Dørvrider");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 11,
                column: "Navn",
                value: "Skilt 1");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 12,
                column: "Navn",
                value: "Låskasse 2");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 13,
                column: "Navn",
                value: "Sluttstykke 2");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 14,
                column: "Navn",
                value: "Sluttstykke 2 utstyr");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 15,
                column: "Navn",
                value: "Sylinder 2 utv");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 16,
                column: "Navn",
                value: "Sylinder 2 innv");

            migrationBuilder.InsertData(
                table: "ComponentTypes",
                columns: new[] { "Id", "Navn" },
                values: new object[,]
                {
                    { 17, "Sylinder 2 utstyr" },
                    { 18, "Sylinder 2 utstyr 2" },
                    { 19, "Skilt 2" },
                    { 20, "Sylinderskruer" },
                    { 21, "Dørautomatikk" },
                    { 22, "Dørautomatikk arm/skinne" },
                    { 23, "Klemsikring bakkant" },
                    { 24, "Klemsikring forkant" },
                    { 25, "Kelmsikring karmoverføring" },
                    { 26, "Dørautomatikk utstyr" },
                    { 27, "Dørautomatikk utstyr 2" },
                    { 28, "Dørautomatikk blindplugg" },
                    { 29, "Dørautomatikk utstyr 4" },
                    { 30, "Dørautomatikk utstyr 5" },
                    { 31, "Kortleser inn" },
                    { 32, "Kortleser ut" },
                    { 33, "Kortleser styreenhet" },
                    { 34, "Impulsbryter innv" },
                    { 35, "Impulsbryter utv." },
                    { 36, "Impulsbryter utstyr" },
                    { 37, "Nøkkelbryter" },
                    { 38, "Nkl.bryter sylinder" },
                    { 39, "Dørlukker aktiv fløy" },
                    { 40, "Dørlukker arm/skinne" },
                    { 41, "Dørlukker passiv fløy" },
                    { 42, "Dørlukker utstyr" },
                    { 43, "Dørlukker utstyr 2" },
                    { 44, "Panikkbeslag/Skåte" },
                    { 45, "Panikkbeslag utstyr" },
                    { 46, "Panikkbeslag utstyr 2" },
                    { 47, "Panikkbeslag utstyr 3" },
                    { 48, "Panikkbeslag utstyr 4" },
                    { 49, "Panikkbeslag utstyr 5" },
                    { 50, "Magnetlås passiv fløy" },
                    { 51, "Magnetlås utstyr" },
                    { 52, "Magnetlås utstyr 2" },
                    { 53, "Nødutstyr mekanisk" },
                    { 54, "Nødutstyr elektrisk" },
                    { 55, "Karmoverføring aktiv fløy" },
                    { 56, "Karmoverføring passiv fløy" },
                    { 57, "Kabel" },
                    { 58, "Dørstopper" },
                    { 59, "Magnetkontakt" },
                    { 60, "Grensesnittboks" },
                    { 61, "Grensesnittboks utstyr 1" },
                    { 62, "Grensesnittboks utstyr 2" },
                    { 63, "Diverse 1" },
                    { 64, "Diverse 2" },
                    { 65, "Diverse 3" }
                });

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Navn",
                value: "Type dør");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Navn",
                value: "Hvilke bruk");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 3,
                column: "Navn",
                value: "FG");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Navn",
                value: "Risikoklasse");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Navn",
                value: "Lukkefunksjon");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Navn",
                value: "Antall fløyer");

            migrationBuilder.InsertData(
                table: "RequirementDimensions",
                columns: new[] { "Id", "Navn", "Rekkefolge" },
                values: new object[,]
                {
                    { 7, "Type beslag", 7 },
                    { 8, "Tilbakerømning", 8 },
                    { 9, "Postsonesylinder", 9 }
                });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 1, 2, "Låsbar" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 2, "Ikke låsbar" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 3, "Toalett" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 4, "Forberedt for kortleser" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 5, 2, "Med kortleser" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 6, 2, "Med dørbladmontert kortleser" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 7, 2, "Kun rømning RK 1-4" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 8, 2, "Kun rømning RK 1-6" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 1, "Ikke FG" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 2, "FG" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 13,
                column: "Verdi",
                value: "Ikke rømning");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 14,
                column: "Verdi",
                value: "1-4");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 15,
                column: "Verdi",
                value: "1-6");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 16,
                column: "Verdi",
                value: "Ingen lukker");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 17,
                column: "Verdi",
                value: "Dørlukker");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 18,
                column: "Verdi",
                value: "Automatikk");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 19,
                column: "Verdi",
                value: "Dørlukker/automatikk");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 1, 6, "1-fløyet" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 2, "2-fløyet" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 3, "Skyvedør" });

            migrationBuilder.InsertData(
                table: "RequirementValues",
                columns: new[] { "Id", "Kode", "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[,]
                {
                    { 23, null, 1, 7, "Mekanisk" },
                    { 24, null, 2, 7, "Elektrisk" },
                    { 25, null, 3, 7, "Lukket/låst signal" },
                    { 26, null, 4, 7, "Hengelås" },
                    { 27, null, 1, 8, "Nei" },
                    { 28, null, 2, 8, "Ja" },
                    { 29, null, 3, 8, "Ikke aktuelt" },
                    { 30, null, 1, 9, "Nei" },
                    { 31, null, 2, 9, "Ja" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Navn",
                value: "Dørblad");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Navn",
                value: "Karm");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Navn",
                value: "Hengsler");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Navn",
                value: "Terskel");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Navn",
                value: "Låskasse");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Navn",
                value: "Sluttstykke");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Navn",
                value: "Dørvrider");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "Navn",
                value: "Sylinder/lås");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 9,
                column: "Navn",
                value: "Panikkbeslag");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Navn",
                value: "Dørpumpe");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 11,
                column: "Navn",
                value: "Dørautomatikk");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 12,
                column: "Navn",
                value: "Koordinering");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 13,
                column: "Navn",
                value: "Kortleser");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 14,
                column: "Navn",
                value: "Albuebryter");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 15,
                column: "Navn",
                value: "Utspaseringsknapp");

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 16,
                column: "Navn",
                value: "Koblingsboks");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Navn",
                value: "Dørtype");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Navn",
                value: "Brannklasse");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 3,
                column: "Navn",
                value: "Risikoklasse");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Navn",
                value: "Rømningskrav");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Navn",
                value: "Sikkerhetsklasse");

            migrationBuilder.UpdateData(
                table: "RequirementDimensions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Navn",
                value: "Automatisk døråpner");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 3, 1, "Branndør" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 1, "Ingen" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 2, "EI30" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 3, "EI60" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 1, 3, "RKL1" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 2, 3, "RKL2" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 3, 3, "RKL3" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 4, 3, "RKL4" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 5, "RKL5" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 6, "RKL6" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 13,
                column: "Verdi",
                value: "Rømning");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 14,
                column: "Verdi",
                value: "Ikke rømning");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 15,
                column: "Verdi",
                value: "Tilbakerømning");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 16,
                column: "Verdi",
                value: "RC1");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 17,
                column: "Verdi",
                value: "RC2");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 18,
                column: "Verdi",
                value: "RC3");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 19,
                column: "Verdi",
                value: "RC4");

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Rekkefolge", "RequirementDimensionId", "Verdi" },
                values: new object[] { 5, 5, "RC5" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 1, "Ja" });

            migrationBuilder.UpdateData(
                table: "RequirementValues",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Rekkefolge", "Verdi" },
                values: new object[] { 2, "Nei" });
        }
    }
}
