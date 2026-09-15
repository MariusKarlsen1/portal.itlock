using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class BackfillProdusentFraLeverandor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Leverandøren er produsenten for de aller fleste katalogene som
            // er importert (Dormakaba, Assa Abloy osv. selger sin egen
            // produksjon direkte) - fyller Produsent fra Leverandor (som
            // alltid speiler leverandørkortet satt som standard, se
            // LeverandorSync) på alle varer som mangler Produsent fra før.
            migrationBuilder.Sql(
                """
                UPDATE Components
                SET Produsent = Leverandor
                WHERE (Produsent IS NULL OR TRIM(Produsent) = '')
                  AND Leverandor IS NOT NULL AND TRIM(Leverandor) != '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
