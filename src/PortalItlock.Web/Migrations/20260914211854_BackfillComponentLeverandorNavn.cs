using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class BackfillComponentLeverandorNavn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Leverandørens eget produktnavn (Navn på lenken) sto tomt på alle
            // koblinger opprettet før feltet fantes - fyller dem med varens
            // eget Varenavn 1 som utgangspunkt, siden de som oftest er like.
            migrationBuilder.Sql(
                """
                UPDATE ComponentLeverandorer
                SET Navn = (SELECT Navn FROM Components WHERE Components.Id = ComponentLeverandorer.ComponentId)
                WHERE Navn IS NULL OR TRIM(Navn) = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
