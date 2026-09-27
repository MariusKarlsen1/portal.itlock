using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilLeverandorLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "Leverandorer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoData",
                table: "Leverandorer",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoFilnavn",
                table: "Leverandorer",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "Leverandorer");

            migrationBuilder.DropColumn(
                name: "LogoData",
                table: "Leverandorer");

            migrationBuilder.DropColumn(
                name: "LogoFilnavn",
                table: "Leverandorer");
        }
    }
}
