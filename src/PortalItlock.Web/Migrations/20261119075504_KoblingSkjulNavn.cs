using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingSkjulNavn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SkjulNavn",
                table: "KoblingsSymboler",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SkjulNavn",
                table: "KoblingsStreker",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Fri flytting som standard: komponentene legger seg der de slippes.
            migrationBuilder.Sql("UPDATE KoblingsSkjemaer SET SnapTilRutenett = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkjulNavn",
                table: "KoblingsSymboler");

            migrationBuilder.DropColumn(
                name: "SkjulNavn",
                table: "KoblingsStreker");
        }
    }
}
