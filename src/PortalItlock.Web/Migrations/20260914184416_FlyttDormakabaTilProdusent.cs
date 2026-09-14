using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class FlyttDormakabaTilProdusent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "Dormakaba" er produsenten, ikke leverandøren - flytter derfor den
            // verdien fra Leverandor over til Produsent på alle varer som har
            // den stående der fra tidligere import, og nullstiller Leverandor.
            // Leverandørkortet (se LeverandorSync) er nå eneste kilde til
            // Leverandor-feltet fremover - settes når en admin faktisk kobler
            // varen til en reell leverandør og markerer den som standard.
            migrationBuilder.Sql(
                """
                UPDATE Components
                SET Produsent = 'Dormakaba'
                WHERE Leverandor = 'Dormakaba' AND (Produsent IS NULL OR TRIM(Produsent) = '');
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM ComponentLeverandorer
                WHERE LeverandorId IN (SELECT Id FROM Leverandorer WHERE Navn = 'Dormakaba');
                """);

            migrationBuilder.Sql("DELETE FROM Leverandorer WHERE Navn = 'Dormakaba';");

            migrationBuilder.Sql("UPDATE Components SET Leverandor = NULL WHERE Leverandor = 'Dormakaba';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
