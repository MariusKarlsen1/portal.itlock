using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ListevisningSomStandard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Liste er standardvisning for alle: fjerner lagret visning (kanban/liste) på
            // arbeidsordre, prosjekter, tickets, CE og systemregister, så alle starter på
            // Liste og kan velge Kanban selv. Andre preferanser (bredde osv.) beholdes.
            migrationBuilder.Sql("""
                UPDATE Brukere
                SET VisningsPreferanser = json_remove(VisningsPreferanser, '$.arbeidsordre', '$.prosjekter', '$.tickets', '$."ce-visning"', '$.systemregister')
                WHERE VisningsPreferanser IS NOT NULL AND json_valid(VisningsPreferanser);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
