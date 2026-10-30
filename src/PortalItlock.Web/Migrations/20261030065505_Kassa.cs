using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Kassa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KasseDagsoppgjor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Lukket = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LukketAv = table.Column<string>(type: "TEXT", nullable: true),
                    KasseNavn = table.Column<string>(type: "TEXT", nullable: true),
                    FraNummer = table.Column<int>(type: "INTEGER", nullable: true),
                    TilNummer = table.Column<int>(type: "INTEGER", nullable: true),
                    AntallSalg = table.Column<int>(type: "INTEGER", nullable: false),
                    AntallKreditnota = table.Column<int>(type: "INTEGER", nullable: false),
                    Omsetning = table.Column<decimal>(type: "TEXT", nullable: false),
                    Mva = table.Column<decimal>(type: "TEXT", nullable: false),
                    KortIKassa = table.Column<decimal>(type: "TEXT", nullable: false),
                    KontantIKassa = table.Column<decimal>(type: "TEXT", nullable: false),
                    TerminalTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    VekselStart = table.Column<decimal>(type: "TEXT", nullable: false),
                    Opptalt = table.Column<decimal>(type: "TEXT", nullable: false),
                    TellingJson = table.Column<string>(type: "TEXT", nullable: true),
                    DifferanseKort = table.Column<decimal>(type: "TEXT", nullable: false),
                    DifferanseKontant = table.Column<decimal>(type: "TEXT", nullable: false),
                    Forklaring = table.Column<string>(type: "TEXT", nullable: true),
                    UtbetaltBank = table.Column<decimal>(type: "TEXT", nullable: true),
                    SattInnBank = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KasseDagsoppgjor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KasseOppsett",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KasseNavn = table.Column<string>(type: "TEXT", nullable: false),
                    Vekselbeholdning = table.Column<decimal>(type: "TEXT", nullable: false),
                    MvaRegistrert = table.Column<bool>(type: "INTEGER", nullable: false),
                    Bunntekst = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KasseOppsett", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KasseVarer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Kategori = table.Column<string>(type: "TEXT", nullable: false),
                    PrisInklMva = table.Column<decimal>(type: "TEXT", nullable: false),
                    MvaSats = table.Column<decimal>(type: "TEXT", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Strekkode = table.Column<string>(type: "TEXT", nullable: true),
                    Ikon = table.Column<string>(type: "TEXT", nullable: false),
                    Favoritt = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KasseVarer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KasseVarer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KasseSalg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    BetaltTilbakeMed = table.Column<int>(type: "INTEGER", nullable: true),
                    KrediterSalgId = table.Column<int>(type: "INTEGER", nullable: true),
                    Arsak = table.Column<string>(type: "TEXT", nullable: true),
                    KundeNavn = table.Column<string>(type: "TEXT", nullable: true),
                    KundeEpost = table.Column<string>(type: "TEXT", nullable: true),
                    RabattProsent = table.Column<decimal>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", nullable: false),
                    Mva = table.Column<decimal>(type: "TEXT", nullable: false),
                    Mottatt = table.Column<decimal>(type: "TEXT", nullable: true),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrukerNavn = table.Column<string>(type: "TEXT", nullable: true),
                    KasseNavn = table.Column<string>(type: "TEXT", nullable: true),
                    DagsoppgjorId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KasseSalg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KasseSalg_KasseDagsoppgjor_DagsoppgjorId",
                        column: x => x.DagsoppgjorId,
                        principalTable: "KasseDagsoppgjor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KasseSalg_KasseSalg_KrediterSalgId",
                        column: x => x.KrediterSalgId,
                        principalTable: "KasseSalg",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KasseSalgLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KasseSalgId = table.Column<int>(type: "INTEGER", nullable: false),
                    KasseVareId = table.Column<int>(type: "INTEGER", nullable: true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Kategori = table.Column<string>(type: "TEXT", nullable: true),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Enhetspris = table.Column<decimal>(type: "TEXT", nullable: false),
                    MvaSats = table.Column<decimal>(type: "TEXT", nullable: false),
                    RabattProsent = table.Column<decimal>(type: "TEXT", nullable: false),
                    Belop = table.Column<decimal>(type: "TEXT", nullable: false),
                    Mva = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KasseSalgLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KasseSalgLinjer_KasseSalg_KasseSalgId",
                        column: x => x.KasseSalgId,
                        principalTable: "KasseSalg",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });


            migrationBuilder.CreateIndex(
                name: "IX_KasseSalg_DagsoppgjorId",
                table: "KasseSalg",
                column: "DagsoppgjorId");

            migrationBuilder.CreateIndex(
                name: "IX_KasseSalg_KrediterSalgId",
                table: "KasseSalg",
                column: "KrediterSalgId");

            migrationBuilder.CreateIndex(
                name: "IX_KasseSalg_Nummer",
                table: "KasseSalg",
                column: "Nummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KasseSalgLinjer_KasseSalgId",
                table: "KasseSalgLinjer",
                column: "KasseSalgId");

            migrationBuilder.CreateIndex(
                name: "IX_KasseVarer_ComponentId",
                table: "KasseVarer",
                column: "ComponentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KasseOppsett");

            migrationBuilder.DropTable(
                name: "KasseSalgLinjer");

            migrationBuilder.DropTable(
                name: "KasseVarer");

            migrationBuilder.DropTable(
                name: "KasseSalg");

            migrationBuilder.DropTable(
                name: "KasseDagsoppgjor");

        }
    }
}
