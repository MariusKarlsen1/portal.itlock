using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Fellesskap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bakgrunn",
                table: "Kunngjoringer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BesteSvarKommentarId",
                table: "Kunngjoringer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FellesskapId",
                table: "Kunngjoringer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KreverBekreftelse",
                table: "Kunngjoringer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RosMerke",
                table: "Kunngjoringer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RosTilBrukerId",
                table: "Kunngjoringer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Kunngjoringer",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AvstemningValg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KunngjoringId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvstemningValg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvstemningValg_Kunngjoringer_KunngjoringId",
                        column: x => x.KunngjoringId,
                        principalTable: "Kunngjoringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fellesskap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Farge = table.Column<string>(type: "TEXT", nullable: false),
                    Tema = table.Column<string>(type: "TEXT", nullable: false),
                    ErObligatorisk = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpprettetAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OmslagData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    OmslagContentType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fellesskap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fellesskap_Brukere_OpprettetAvBrukerId",
                        column: x => x.OpprettetAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KunngjoringBekreftelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KunngjoringId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KunngjoringBekreftelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KunngjoringBekreftelser_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KunngjoringBekreftelser_Kunngjoringer_KunngjoringId",
                        column: x => x.KunngjoringId,
                        principalTable: "Kunngjoringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KunngjoringVisninger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KunngjoringId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KunngjoringVisninger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KunngjoringVisninger_Kunngjoringer_KunngjoringId",
                        column: x => x.KunngjoringId,
                        principalTable: "Kunngjoringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvstemningStemmer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ValgId = table.Column<int>(type: "INTEGER", nullable: false),
                    KunngjoringId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvstemningStemmer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvstemningStemmer_AvstemningValg_ValgId",
                        column: x => x.ValgId,
                        principalTable: "AvstemningValg",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FellesskapMedlemmer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FellesskapId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ErAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErFavoritt = table.Column<bool>(type: "INTEGER", nullable: false),
                    BleMedDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FellesskapMedlemmer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FellesskapMedlemmer_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FellesskapMedlemmer_Fellesskap_FellesskapId",
                        column: x => x.FellesskapId,
                        principalTable: "Fellesskap",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kunngjoringer_FellesskapId",
                table: "Kunngjoringer",
                column: "FellesskapId");

            migrationBuilder.CreateIndex(
                name: "IX_Kunngjoringer_RosTilBrukerId",
                table: "Kunngjoringer",
                column: "RosTilBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_AvstemningStemmer_KunngjoringId_BrukerId",
                table: "AvstemningStemmer",
                columns: new[] { "KunngjoringId", "BrukerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvstemningStemmer_ValgId",
                table: "AvstemningStemmer",
                column: "ValgId");

            migrationBuilder.CreateIndex(
                name: "IX_AvstemningValg_KunngjoringId",
                table: "AvstemningValg",
                column: "KunngjoringId");

            migrationBuilder.CreateIndex(
                name: "IX_Fellesskap_OpprettetAvBrukerId",
                table: "Fellesskap",
                column: "OpprettetAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_FellesskapMedlemmer_BrukerId",
                table: "FellesskapMedlemmer",
                column: "BrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_FellesskapMedlemmer_FellesskapId_BrukerId",
                table: "FellesskapMedlemmer",
                columns: new[] { "FellesskapId", "BrukerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringBekreftelser_BrukerId",
                table: "KunngjoringBekreftelser",
                column: "BrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringBekreftelser_KunngjoringId_BrukerId",
                table: "KunngjoringBekreftelser",
                columns: new[] { "KunngjoringId", "BrukerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringVisninger_KunngjoringId_BrukerId",
                table: "KunngjoringVisninger",
                columns: new[] { "KunngjoringId", "BrukerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Kunngjoringer_Brukere_RosTilBrukerId",
                table: "Kunngjoringer",
                column: "RosTilBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Kunngjoringer_Fellesskap_FellesskapId",
                table: "Kunngjoringer",
                column: "FellesskapId",
                principalTable: "Fellesskap",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kunngjoringer_Brukere_RosTilBrukerId",
                table: "Kunngjoringer");

            migrationBuilder.DropForeignKey(
                name: "FK_Kunngjoringer_Fellesskap_FellesskapId",
                table: "Kunngjoringer");

            migrationBuilder.DropTable(
                name: "AvstemningStemmer");

            migrationBuilder.DropTable(
                name: "FellesskapMedlemmer");

            migrationBuilder.DropTable(
                name: "KunngjoringBekreftelser");

            migrationBuilder.DropTable(
                name: "KunngjoringVisninger");

            migrationBuilder.DropTable(
                name: "AvstemningValg");

            migrationBuilder.DropTable(
                name: "Fellesskap");

            migrationBuilder.DropIndex(
                name: "IX_Kunngjoringer_FellesskapId",
                table: "Kunngjoringer");

            migrationBuilder.DropIndex(
                name: "IX_Kunngjoringer_RosTilBrukerId",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "Bakgrunn",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "BesteSvarKommentarId",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "FellesskapId",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "KreverBekreftelse",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "RosMerke",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "RosTilBrukerId",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Kunngjoringer");
        }
    }
}
