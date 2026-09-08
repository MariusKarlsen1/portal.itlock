using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilNavn2OgMontasjeblad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MontasjebladContentType",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "MontasjebladData",
                table: "Components",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MontasjebladFilnavn",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Navn2",
                table: "Components",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontasjebladContentType",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "MontasjebladData",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "MontasjebladFilnavn",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "Navn2",
                table: "Components");
        }
    }
}
