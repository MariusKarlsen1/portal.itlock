using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KoblingStrekTekst : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TekstSlutt",
                table: "KoblingsStreker",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TekstStart",
                table: "KoblingsStreker",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TekstSlutt",
                table: "KoblingsStreker");

            migrationBuilder.DropColumn(
                name: "TekstStart",
                table: "KoblingsStreker");
        }
    }
}
