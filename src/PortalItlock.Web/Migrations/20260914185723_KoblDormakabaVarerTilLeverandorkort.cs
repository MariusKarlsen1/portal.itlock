using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblDormakabaVarerTilLeverandorkort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Varer som importeres under en produsent skal ligge på et
            // leverandørkort med samme navn - gjenoppretter derfor
            // "Dormakaba" som leverandørkort (fjernet i forrige migrasjon da
            // den lå feilplassert i Leverandor-feltet) og kobler alle varer
            // med Produsent = 'Dormakaba' til den, satt som standard siden
            // ingen annen leverandør er valgt for dem ennå.
            migrationBuilder.Sql(
                """
                INSERT INTO Leverandorer (Navn)
                SELECT 'Dormakaba' WHERE NOT EXISTS (SELECT 1 FROM Leverandorer WHERE Navn = 'Dormakaba');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO ComponentLeverandorer (ComponentId, LeverandorId, Varenummer, Pris, ErStandard)
                SELECT c.Id, l.Id, c.Produktkode, c.PrisNetto, 1
                FROM Components c
                JOIN Leverandorer l ON l.Navn = 'Dormakaba'
                WHERE c.Produsent = 'Dormakaba'
                AND NOT EXISTS (
                    SELECT 1 FROM ComponentLeverandorer cl
                    WHERE cl.ComponentId = c.Id AND cl.LeverandorId = l.Id
                );
                """);

            migrationBuilder.Sql("UPDATE Components SET Leverandor = 'Dormakaba' WHERE Produsent = 'Dormakaba';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
