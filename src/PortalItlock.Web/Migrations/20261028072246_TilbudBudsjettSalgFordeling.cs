using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudBudsjettSalgFordeling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SalgAndre",
                table: "TilbudBudsjetter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalgArbeid",
                table: "TilbudBudsjetter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalgVarer",
                table: "TilbudBudsjetter",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalgAndre",
                table: "TilbudBudsjetter");

            migrationBuilder.DropColumn(
                name: "SalgArbeid",
                table: "TilbudBudsjetter");

            migrationBuilder.DropColumn(
                name: "SalgVarer",
                table: "TilbudBudsjetter");
        }
    }
}
