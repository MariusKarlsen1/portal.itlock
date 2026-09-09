using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilRessursplan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ressursplaner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Info = table.Column<string>(type: "TEXT", nullable: false),
                    OpprettetAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ressursplaner", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ressursplaner_Brukere_MontorId",
                        column: x => x.MontorId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ressursplaner_Brukere_OpprettetAvBrukerId",
                        column: x => x.OpprettetAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ressursplaner_MontorId",
                table: "Ressursplaner",
                column: "MontorId");

            migrationBuilder.CreateIndex(
                name: "IX_Ressursplaner_OpprettetAvBrukerId",
                table: "Ressursplaner",
                column: "OpprettetAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ressursplaner");
        }
    }
}
