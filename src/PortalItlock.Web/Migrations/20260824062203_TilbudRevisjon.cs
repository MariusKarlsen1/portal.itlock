using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudRevisjon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TilbudRevisjoner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TilbudId = table.Column<int>(type: "INTEGER", nullable: false),
                    Versjonsnummer = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    PrisType = table.Column<int>(type: "INTEGER", nullable: false),
                    Prosentsats = table.Column<decimal>(type: "TEXT", nullable: false),
                    Timepris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Montasjekost = table.Column<decimal>(type: "TEXT", nullable: true),
                    LinjerJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilbudRevisjoner", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TilbudRevisjoner_Tilbud_TilbudId",
                        column: x => x.TilbudId,
                        principalTable: "Tilbud",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TilbudRevisjoner_TilbudId",
                table: "TilbudRevisjoner",
                column: "TilbudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TilbudRevisjoner");
        }
    }
}
