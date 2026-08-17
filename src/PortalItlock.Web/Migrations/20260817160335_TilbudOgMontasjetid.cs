using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudOgMontasjetid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MontasjeMinutter",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tilbud",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    PrisType = table.Column<int>(type: "INTEGER", nullable: false),
                    Prosentsats = table.Column<decimal>(type: "TEXT", nullable: false),
                    Timepris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Montasjekost = table.Column<decimal>(type: "TEXT", nullable: true),
                    Forside = table.Column<string>(type: "TEXT", nullable: true),
                    VisEnhetspris = table.Column<bool>(type: "INTEGER", nullable: false),
                    VisProduktkode = table.Column<bool>(type: "INTEGER", nullable: false),
                    VisKunTotalsum = table.Column<bool>(type: "INTEGER", nullable: false),
                    VisKunTotaltUtenMva = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OppdatertDato = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tilbud", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tilbud_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TilbudLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TilbudId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Innpris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Utpris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    MontasjeMinutter = table.Column<int>(type: "INTEGER", nullable: true),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilbudLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TilbudLinjer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TilbudLinjer_Tilbud_TilbudId",
                        column: x => x.TilbudId,
                        principalTable: "Tilbud",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tilbud_ProsjektId",
                table: "Tilbud",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_TilbudLinjer_ComponentId",
                table: "TilbudLinjer",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_TilbudLinjer_TilbudId",
                table: "TilbudLinjer",
                column: "TilbudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TilbudLinjer");

            migrationBuilder.DropTable(
                name: "Tilbud");

            migrationBuilder.DropColumn(
                name: "MontasjeMinutter",
                table: "Components");
        }
    }
}
