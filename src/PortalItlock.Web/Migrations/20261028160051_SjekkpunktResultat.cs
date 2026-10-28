using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class SjekkpunktResultat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kommentar",
                table: "ArbeidsordreSjekkpunkter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Resultat",
                table: "ArbeidsordreSjekkpunkter",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kommentar",
                table: "ArbeidsordreSjekkpunkter");

            migrationBuilder.DropColumn(
                name: "Resultat",
                table: "ArbeidsordreSjekkpunkter");
        }
    }
}
