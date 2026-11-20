using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudTransport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ErTransport",
                table: "TilbudKodeLinjer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Kjoringer",
                table: "TilbudKodeLinjer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KmPerTur",
                table: "TilbudKodeLinjer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TransportAuto",
                table: "TilbudKodeLinjer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErTransport",
                table: "TilbudKodeLinjer");

            migrationBuilder.DropColumn(
                name: "Kjoringer",
                table: "TilbudKodeLinjer");

            migrationBuilder.DropColumn(
                name: "KmPerTur",
                table: "TilbudKodeLinjer");

            migrationBuilder.DropColumn(
                name: "TransportAuto",
                table: "TilbudKodeLinjer");
        }
    }
}
