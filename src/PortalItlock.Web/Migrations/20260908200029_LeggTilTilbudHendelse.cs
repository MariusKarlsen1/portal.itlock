using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTilbudHendelse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TilbudHendelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TilbudId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    UtfortAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilbudHendelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TilbudHendelser_Brukere_UtfortAvBrukerId",
                        column: x => x.UtfortAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TilbudHendelser_Tilbud_TilbudId",
                        column: x => x.TilbudId,
                        principalTable: "Tilbud",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TilbudHendelser_TilbudId",
                table: "TilbudHendelser",
                column: "TilbudId");

            migrationBuilder.CreateIndex(
                name: "IX_TilbudHendelser_UtfortAvBrukerId",
                table: "TilbudHendelser",
                column: "UtfortAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TilbudHendelser");
        }
    }
}
