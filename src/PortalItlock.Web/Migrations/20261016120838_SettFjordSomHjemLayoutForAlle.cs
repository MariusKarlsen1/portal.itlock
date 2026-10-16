using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class SettFjordSomHjemLayoutForAlle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Layout 3 (HjemLayout.Fjord = 3) er ny standardforside for alle,
            // også de som allerede har en bruker. Samme fremgangsmåte som
            // SettDashboard3SomHjemLayoutForAlle: en ren visningspreferanse, og
            // den som vil ha en annen layout bytter selv i Tilpass meny.
            migrationBuilder.Sql("UPDATE Brukere SET HjemLayoutValg = 3;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Bevisst tom - de gamle valgene er ikke lagret noe sted (se
            // SettDashboard3SomHjemLayoutForAlle).
        }
    }
}
