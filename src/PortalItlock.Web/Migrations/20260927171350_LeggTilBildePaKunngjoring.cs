using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilBildePaKunngjoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BildeContentType",
                table: "Kunngjoringer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BildeData",
                table: "Kunngjoringer",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BildeFilnavn",
                table: "Kunngjoringer",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BildeContentType",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "BildeData",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "BildeFilnavn",
                table: "Kunngjoringer");
        }
    }
}
