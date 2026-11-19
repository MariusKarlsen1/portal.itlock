using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudOpsjonOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpsjonGruppering",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpsjonOverskrift",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpsjonSortering",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpsjonGruppering",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "OpsjonOverskrift",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "OpsjonSortering",
                table: "Tilbud");
        }
    }
}
