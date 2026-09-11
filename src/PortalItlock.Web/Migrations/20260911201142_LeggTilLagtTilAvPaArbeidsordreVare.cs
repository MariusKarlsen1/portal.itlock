using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilLagtTilAvPaArbeidsordreVare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LagtTilAvBrukerId",
                table: "ArbeidsordreVarer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LagtTilDato",
                table: "ArbeidsordreVarer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreVarer_LagtTilAvBrukerId",
                table: "ArbeidsordreVarer",
                column: "LagtTilAvBrukerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArbeidsordreVarer_Brukere_LagtTilAvBrukerId",
                table: "ArbeidsordreVarer",
                column: "LagtTilAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArbeidsordreVarer_Brukere_LagtTilAvBrukerId",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropIndex(
                name: "IX_ArbeidsordreVarer_LagtTilAvBrukerId",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropColumn(
                name: "LagtTilAvBrukerId",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropColumn(
                name: "LagtTilDato",
                table: "ArbeidsordreVarer");
        }
    }
}
