using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTilvalgMal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TilvalgMaler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilvalgMaler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TilvalgMalAlternativer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TilvalgMalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false),
                    BildeData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    BildeContentType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilvalgMalAlternativer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TilvalgMalAlternativer_TilvalgMaler_TilvalgMalId",
                        column: x => x.TilvalgMalId,
                        principalTable: "TilvalgMaler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TilvalgMalAlternativer_TilvalgMalId",
                table: "TilvalgMalAlternativer",
                column: "TilvalgMalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TilvalgMalAlternativer");

            migrationBuilder.DropTable(
                name: "TilvalgMaler");
        }
    }
}
