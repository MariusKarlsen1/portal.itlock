using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilBildePaKomponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BildeContentType",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BildeData",
                table: "Components",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BildeFilnavn",
                table: "Components",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BildeContentType",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "BildeData",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "BildeFilnavn",
                table: "Components");
        }
    }
}
