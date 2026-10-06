using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKanalOgNotatPaForesporsel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kanal",
                table: "Foresporsler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ForesporselNotater",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ForesporselId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    OpprettetAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForesporselNotater", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForesporselNotater_Brukere_OpprettetAvBrukerId",
                        column: x => x.OpprettetAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ForesporselNotater_Foresporsler_ForesporselId",
                        column: x => x.ForesporselId,
                        principalTable: "Foresporsler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForesporselNotater_ForesporselId",
                table: "ForesporselNotater",
                column: "ForesporselId");

            migrationBuilder.CreateIndex(
                name: "IX_ForesporselNotater_OpprettetAvBrukerId",
                table: "ForesporselNotater",
                column: "OpprettetAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForesporselNotater");

            migrationBuilder.DropColumn(
                name: "Kanal",
                table: "Foresporsler");
        }
    }
}
