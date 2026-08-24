using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class EndringsmeldingPaTilbud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OpprinneligTilbudId",
                table: "Tilbud",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tilbud_OpprinneligTilbudId",
                table: "Tilbud",
                column: "OpprinneligTilbudId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tilbud_Tilbud_OpprinneligTilbudId",
                table: "Tilbud",
                column: "OpprinneligTilbudId",
                principalTable: "Tilbud",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tilbud_Tilbud_OpprinneligTilbudId",
                table: "Tilbud");

            migrationBuilder.DropIndex(
                name: "IX_Tilbud_OpprinneligTilbudId",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "OpprinneligTilbudId",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Tilbud");
        }
    }
}
