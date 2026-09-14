using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKundePaArbeidsordre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KundeId",
                table: "Arbeidsordre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Arbeidsordre_KundeId",
                table: "Arbeidsordre",
                column: "KundeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Arbeidsordre_Kunder_KundeId",
                table: "Arbeidsordre",
                column: "KundeId",
                principalTable: "Kunder",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Arbeidsordre_Kunder_KundeId",
                table: "Arbeidsordre");

            migrationBuilder.DropIndex(
                name: "IX_Arbeidsordre_KundeId",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "KundeId",
                table: "Arbeidsordre");
        }
    }
}
