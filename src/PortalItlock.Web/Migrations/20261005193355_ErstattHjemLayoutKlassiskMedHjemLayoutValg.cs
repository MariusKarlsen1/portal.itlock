using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ErstattHjemLayoutKlassiskMedHjemLayoutValg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HjemLayoutKlassisk",
                table: "Brukere",
                newName: "HjemLayoutValg");

            // Gamle verdier var en bool (1 = Klassisk, 0 = Bilde). Nye verdier
            // er en enum (0 = Klassisk, 1 = Bilde, 2 = Dashboard3) - uten
            // denne swappen ville alle som hadde valgt Klassisk (1) plutselig
            // vist Bilde, og omvendt.
            migrationBuilder.Sql(
                "UPDATE Brukere SET HjemLayoutValg = CASE HjemLayoutValg WHEN 1 THEN 0 WHEN 0 THEN 1 ELSE HjemLayoutValg END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Brukere SET HjemLayoutValg = CASE HjemLayoutValg WHEN 0 THEN 1 WHEN 1 THEN 0 ELSE 0 END;");

            migrationBuilder.RenameColumn(
                name: "HjemLayoutValg",
                table: "Brukere",
                newName: "HjemLayoutKlassisk");
        }
    }
}
