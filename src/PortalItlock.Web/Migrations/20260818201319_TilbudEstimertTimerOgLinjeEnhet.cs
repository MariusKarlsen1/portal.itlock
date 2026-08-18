using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudEstimertTimerOgLinjeEnhet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Enhet",
                table: "TilbudLinjer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimertTimerOverride",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enhet",
                table: "TilbudLinjer");

            migrationBuilder.DropColumn(
                name: "EstimertTimerOverride",
                table: "Tilbud");
        }
    }
}
