using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class CeDekningsomradeOgEkstraFunksjoner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EkstraFunksjonerTestet",
                table: "CeGodkjenninger");

            migrationBuilder.AddColumn<double>(
                name: "BeskyttetBreddeHovedDorbladMm",
                table: "CeGodkjenninger",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DekningsomradeHovedDorblad",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EkstraFunksjonerKommentar",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeskyttetBreddeHovedDorbladMm",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "DekningsomradeHovedDorblad",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "EkstraFunksjonerKommentar",
                table: "CeGodkjenninger");

            migrationBuilder.AddColumn<bool>(
                name: "EkstraFunksjonerTestet",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);
        }
    }
}
