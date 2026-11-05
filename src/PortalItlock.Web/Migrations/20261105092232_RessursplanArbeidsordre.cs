using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class RessursplanArbeidsordre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArbeidsordreId",
                table: "Ressursplaner",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ressursplaner_ArbeidsordreId",
                table: "Ressursplaner",
                column: "ArbeidsordreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ressursplaner_Arbeidsordre_ArbeidsordreId",
                table: "Ressursplaner",
                column: "ArbeidsordreId",
                principalTable: "Arbeidsordre",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ressursplaner_Arbeidsordre_ArbeidsordreId",
                table: "Ressursplaner");

            migrationBuilder.DropIndex(
                name: "IX_Ressursplaner_ArbeidsordreId",
                table: "Ressursplaner");

            migrationBuilder.DropColumn(
                name: "ArbeidsordreId",
                table: "Ressursplaner");
        }
    }
}
