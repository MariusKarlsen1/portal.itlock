using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class SettDashboard3SomHjemLayoutForAlle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alle skal møte den samme forsiden, ikke bare nye brukere: den nye
            // Dashboard3-visningen (HjemLayout.Dashboard3 = 2). Flertallet lå på
            // Bilde (1), som ikke engang er valgbar i "Dashboard-stil" lenger.
            //
            // Ingen mister noe: dette er en ren visningspreferanse, og den som
            // heller vil ha den klassiske forsiden velger Layout 1 i Tilpass
            // meny igjen. Kun denne ene kolonnen røres.
            migrationBuilder.Sql("UPDATE Brukere SET HjemLayoutValg = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Bevisst tom. Up() skriver over hver brukers eget valg, og de gamle
            // verdiene finnes ikke lagret noe sted - en "tilbakestilling" her
            // ville bare gjettet på en felles verdi for alle, og dermed endret
            // preferansen til folk en gang til. Den som vil bytte tilbake gjør
            // det selv i Tilpass meny.
        }
    }
}
