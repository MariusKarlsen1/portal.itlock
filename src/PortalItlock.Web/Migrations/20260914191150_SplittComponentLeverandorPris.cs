using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class SplittComponentLeverandorPris : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Det gamle "Pris"-feltet holdt leverandørens (netto/innkjøps-)
            // pris - gir derfor navnet PrisNetto videre til de eksisterende
            // verdiene, og legger PrisVeiledende til som et nytt, tomt felt.
            migrationBuilder.RenameColumn(
                name: "Pris",
                table: "ComponentLeverandorer",
                newName: "PrisNetto");

            migrationBuilder.AddColumn<decimal>(
                name: "PrisVeiledende",
                table: "ComponentLeverandorer",
                type: "TEXT",
                nullable: true);

            // Fyller PrisVeiledende for eksisterende koblinger fra varens egen
            // veiledende pris, som et rimelig utgangspunkt (samme mønster som
            // backfillen av PrisNetto/Varenummer i tidligere migrasjoner).
            migrationBuilder.Sql(
                """
                UPDATE ComponentLeverandorer
                SET PrisVeiledende = (SELECT c.PrisVeiledende FROM Components c WHERE c.Id = ComponentLeverandorer.ComponentId)
                WHERE PrisVeiledende IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrisVeiledende",
                table: "ComponentLeverandorer");

            migrationBuilder.RenameColumn(
                name: "PrisNetto",
                table: "ComponentLeverandorer",
                newName: "Pris");
        }
    }
}
