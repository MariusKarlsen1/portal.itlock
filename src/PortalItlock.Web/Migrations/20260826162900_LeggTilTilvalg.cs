using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTilvalg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tilvalg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublisertDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BesvartDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LestAvAnsatt = table.Column<bool>(type: "INTEGER", nullable: false),
                    Signatur = table.Column<byte[]>(type: "BLOB", nullable: true),
                    SignertAvNavn = table.Column<string>(type: "TEXT", nullable: true),
                    SumTotal = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tilvalg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tilvalg_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TilvalgAlternativer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TilvalgId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false),
                    BildeData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    BildeContentType = table.Column<string>(type: "TEXT", nullable: true),
                    ValgtAntall = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilvalgAlternativer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TilvalgAlternativer_Tilvalg_TilvalgId",
                        column: x => x.TilvalgId,
                        principalTable: "Tilvalg",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tilvalg_ProsjektId",
                table: "Tilvalg",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_TilvalgAlternativer_TilvalgId",
                table: "TilvalgAlternativer",
                column: "TilvalgId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TilvalgAlternativer");

            migrationBuilder.DropTable(
                name: "Tilvalg");
        }
    }
}
