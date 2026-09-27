using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKunngjoringLikerOgKommentarer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KunngjoringKommentarer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KunngjoringId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KunngjoringKommentarer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KunngjoringKommentarer_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KunngjoringKommentarer_Kunngjoringer_KunngjoringId",
                        column: x => x.KunngjoringId,
                        principalTable: "Kunngjoringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KunngjoringLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KunngjoringId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KunngjoringLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KunngjoringLikes_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KunngjoringLikes_Kunngjoringer_KunngjoringId",
                        column: x => x.KunngjoringId,
                        principalTable: "Kunngjoringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringKommentarer_BrukerId",
                table: "KunngjoringKommentarer",
                column: "BrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringKommentarer_KunngjoringId",
                table: "KunngjoringKommentarer",
                column: "KunngjoringId");

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringLikes_BrukerId",
                table: "KunngjoringLikes",
                column: "BrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringLikes_KunngjoringId_BrukerId",
                table: "KunngjoringLikes",
                columns: new[] { "KunngjoringId", "BrukerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KunngjoringKommentarer");

            migrationBuilder.DropTable(
                name: "KunngjoringLikes");
        }
    }
}
