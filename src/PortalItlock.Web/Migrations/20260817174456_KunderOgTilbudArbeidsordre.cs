using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KunderOgTilbudArbeidsordre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kunde",
                table: "Prosjekter");

            migrationBuilder.AddColumn<int>(
                name: "KundeId",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TilbudId",
                table: "Arbeidsordre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Kunder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", nullable: true),
                    Epost = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    Postnr = table.Column<string>(type: "TEXT", nullable: true),
                    Sted = table.Column<string>(type: "TEXT", nullable: true),
                    Notater = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kunder", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prosjekter_KundeId",
                table: "Prosjekter",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_Arbeidsordre_TilbudId",
                table: "Arbeidsordre",
                column: "TilbudId");

            migrationBuilder.AddForeignKey(
                name: "FK_Arbeidsordre_Tilbud_TilbudId",
                table: "Arbeidsordre",
                column: "TilbudId",
                principalTable: "Tilbud",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Prosjekter_Kunder_KundeId",
                table: "Prosjekter",
                column: "KundeId",
                principalTable: "Kunder",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Arbeidsordre_Tilbud_TilbudId",
                table: "Arbeidsordre");

            migrationBuilder.DropForeignKey(
                name: "FK_Prosjekter_Kunder_KundeId",
                table: "Prosjekter");

            migrationBuilder.DropTable(
                name: "Kunder");

            migrationBuilder.DropIndex(
                name: "IX_Prosjekter_KundeId",
                table: "Prosjekter");

            migrationBuilder.DropIndex(
                name: "IX_Arbeidsordre_TilbudId",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "KundeId",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "TilbudId",
                table: "Arbeidsordre");

            migrationBuilder.AddColumn<string>(
                name: "Kunde",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);
        }
    }
}
