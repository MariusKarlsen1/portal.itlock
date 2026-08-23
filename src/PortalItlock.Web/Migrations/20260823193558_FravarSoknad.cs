using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class FravarSoknad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FravarSoknader",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    FraDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TilDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Kommentar = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BehandletDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BehandletAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    AvslagsBegrunnelse = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FravarSoknader", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FravarSoknader_Brukere_BehandletAvBrukerId",
                        column: x => x.BehandletAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FravarSoknader_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FravarSoknader_BehandletAvBrukerId",
                table: "FravarSoknader",
                column: "BehandletAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_FravarSoknader_BrukerId",
                table: "FravarSoknader",
                column: "BrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FravarSoknader");
        }
    }
}
