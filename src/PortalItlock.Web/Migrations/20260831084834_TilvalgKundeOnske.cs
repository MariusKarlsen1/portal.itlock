using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilvalgKundeOnske : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KundeOnskeBildeContentType",
                table: "Tilvalg",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "KundeOnskeBildeData",
                table: "Tilvalg",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KundeOnskeTekst",
                table: "Tilvalg",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KundeOnskeBildeContentType",
                table: "Tilvalg");

            migrationBuilder.DropColumn(
                name: "KundeOnskeBildeData",
                table: "Tilvalg");

            migrationBuilder.DropColumn(
                name: "KundeOnskeTekst",
                table: "Tilvalg");
        }
    }
}
