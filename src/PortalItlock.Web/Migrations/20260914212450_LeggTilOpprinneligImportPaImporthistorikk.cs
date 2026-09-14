using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilOpprinneligImportPaImporthistorikk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OpprinneligImportId",
                table: "Importhistorikk",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Importhistorikk_OpprinneligImportId",
                table: "Importhistorikk",
                column: "OpprinneligImportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Importhistorikk_Importhistorikk_OpprinneligImportId",
                table: "Importhistorikk",
                column: "OpprinneligImportId",
                principalTable: "Importhistorikk",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Importhistorikk_Importhistorikk_OpprinneligImportId",
                table: "Importhistorikk");

            migrationBuilder.DropIndex(
                name: "IX_Importhistorikk_OpprinneligImportId",
                table: "Importhistorikk");

            migrationBuilder.DropColumn(
                name: "OpprinneligImportId",
                table: "Importhistorikk");
        }
    }
}
