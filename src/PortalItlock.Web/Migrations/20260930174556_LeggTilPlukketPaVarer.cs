using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilPlukketPaVarer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Plukket",
                table: "TilbudLinjer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PlukketAvBrukerId",
                table: "TilbudLinjer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlukketDato",
                table: "TilbudLinjer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Plukket",
                table: "ArbeidsordreVarer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PlukketAvBrukerId",
                table: "ArbeidsordreVarer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlukketDato",
                table: "ArbeidsordreVarer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TilbudLinjer_PlukketAvBrukerId",
                table: "TilbudLinjer",
                column: "PlukketAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreVarer_PlukketAvBrukerId",
                table: "ArbeidsordreVarer",
                column: "PlukketAvBrukerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArbeidsordreVarer_Brukere_PlukketAvBrukerId",
                table: "ArbeidsordreVarer",
                column: "PlukketAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TilbudLinjer_Brukere_PlukketAvBrukerId",
                table: "TilbudLinjer",
                column: "PlukketAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArbeidsordreVarer_Brukere_PlukketAvBrukerId",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropForeignKey(
                name: "FK_TilbudLinjer_Brukere_PlukketAvBrukerId",
                table: "TilbudLinjer");

            migrationBuilder.DropIndex(
                name: "IX_TilbudLinjer_PlukketAvBrukerId",
                table: "TilbudLinjer");

            migrationBuilder.DropIndex(
                name: "IX_ArbeidsordreVarer_PlukketAvBrukerId",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropColumn(
                name: "Plukket",
                table: "TilbudLinjer");

            migrationBuilder.DropColumn(
                name: "PlukketAvBrukerId",
                table: "TilbudLinjer");

            migrationBuilder.DropColumn(
                name: "PlukketDato",
                table: "TilbudLinjer");

            migrationBuilder.DropColumn(
                name: "Plukket",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropColumn(
                name: "PlukketAvBrukerId",
                table: "ArbeidsordreVarer");

            migrationBuilder.DropColumn(
                name: "PlukketDato",
                table: "ArbeidsordreVarer");
        }
    }
}
