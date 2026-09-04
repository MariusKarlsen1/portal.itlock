using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class CeSerienummerGlassSignatur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GlassFareKuttSkade",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GlassSynligTiltak",
                table: "CeGodkjenninger",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Serienummer",
                table: "CeGodkjenninger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "UtfortAvSignatur",
                table: "CeGodkjenninger",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "VerifisertAvSignatur",
                table: "CeGodkjenninger",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlassFareKuttSkade",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "GlassSynligTiltak",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "Serienummer",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "UtfortAvSignatur",
                table: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "VerifisertAvSignatur",
                table: "CeGodkjenninger");
        }
    }
}
