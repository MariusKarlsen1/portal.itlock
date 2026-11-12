using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ArbeidsordreFlereTilbud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArbeidsordreTilbud",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    TilbudId = table.Column<int>(type: "INTEGER", nullable: false),
                    LagtTilDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArbeidsordreTilbud", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArbeidsordreTilbud_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArbeidsordreTilbud_Tilbud_TilbudId",
                        column: x => x.TilbudId,
                        principalTable: "Tilbud",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreTilbud_ArbeidsordreId_TilbudId",
                table: "ArbeidsordreTilbud",
                columns: new[] { "ArbeidsordreId", "TilbudId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreTilbud_TilbudId",
                table: "ArbeidsordreTilbud",
                column: "TilbudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArbeidsordreTilbud");
        }
    }
}
