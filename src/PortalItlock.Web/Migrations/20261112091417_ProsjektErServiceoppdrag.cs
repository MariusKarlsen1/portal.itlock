using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProsjektErServiceoppdrag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ErServiceoppdrag",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Prosjekter laget fra «Tilbud service» før feltet fantes: status
            // Serviceavtale, med tilbud, uten servicerunder, dører eller antall
            // service i året - og ikke koblet som avtale fra et ferdig prosjekt.
            migrationBuilder.Sql(@"
UPDATE Prosjekter SET ErServiceoppdrag = 1
WHERE Status = 5 AND HarServiceavtale = 0 AND ServiceGangerPerAr IS NULL
  AND EXISTS (SELECT 1 FROM Tilbud t WHERE t.ProsjektId = Prosjekter.Id)
  AND NOT EXISTS (SELECT 1 FROM Servicerunder r WHERE r.ProsjektId = Prosjekter.Id)
  AND NOT EXISTS (SELECT 1 FROM Dorer d WHERE d.ProsjektId = Prosjekter.Id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErServiceoppdrag",
                table: "Prosjekter");
        }
    }
}
