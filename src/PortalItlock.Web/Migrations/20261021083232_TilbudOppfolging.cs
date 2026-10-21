using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudOppfolging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OppfolgingBrukerId",
                table: "Tilbud",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OppfolgingDager",
                table: "Tilbud",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OppfolgingSendt",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OppfolgingBrukerId",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "OppfolgingDager",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "OppfolgingSendt",
                table: "Tilbud");
        }
    }
}
