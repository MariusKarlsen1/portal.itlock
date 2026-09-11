using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilFelterPaArbeidsordre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimerteTimer",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FullfortAvBrukerId",
                table: "Arbeidsordre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FullfortDato",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hentehylle",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Arbeidsordre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Arbeidsordre_FullfortAvBrukerId",
                table: "Arbeidsordre",
                column: "FullfortAvBrukerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Arbeidsordre_Brukere_FullfortAvBrukerId",
                table: "Arbeidsordre",
                column: "FullfortAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Arbeidsordre_Brukere_FullfortAvBrukerId",
                table: "Arbeidsordre");

            migrationBuilder.DropIndex(
                name: "IX_Arbeidsordre_FullfortAvBrukerId",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "EstimerteTimer",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "FullfortAvBrukerId",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "FullfortDato",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "Hentehylle",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Arbeidsordre");
        }
    }
}
