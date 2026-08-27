using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilPdfSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BefaringPdfer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BefaringId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BefaringPdfer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BefaringPdfer_Befaringer_BefaringId",
                        column: x => x.BefaringId,
                        principalTable: "Befaringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SjekklistePdfer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SjekklistePdfer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SjekklistePdfer_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BefaringPdfer_BefaringId",
                table: "BefaringPdfer",
                column: "BefaringId");

            migrationBuilder.CreateIndex(
                name: "IX_SjekklistePdfer_ArbeidsordreId",
                table: "SjekklistePdfer",
                column: "ArbeidsordreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BefaringPdfer");

            migrationBuilder.DropTable(
                name: "SjekklistePdfer");
        }
    }
}
