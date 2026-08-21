using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServicerundeDelerOgSjekkliste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServicerundeDeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServicerundeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    Feil = table.Column<string>(type: "TEXT", nullable: true),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicerundeDeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicerundeDeler_Dorer_DorId",
                        column: x => x.DorId,
                        principalTable: "Dorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServicerundeDeler_Servicerunder_ServicerundeId",
                        column: x => x.ServicerundeId,
                        principalTable: "Servicerunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicerundeSjekklistepunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicerundeSjekklistepunkter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicerundeSjekkpunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServicerundeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false),
                    Fullfort = table.Column<bool>(type: "INTEGER", nullable: false),
                    FullfortDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FullfortAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicerundeSjekkpunkter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicerundeSjekkpunkter_Brukere_FullfortAvBrukerId",
                        column: x => x.FullfortAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServicerundeSjekkpunkter_Servicerunder_ServicerundeId",
                        column: x => x.ServicerundeId,
                        principalTable: "Servicerunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicerundeDeler_DorId",
                table: "ServicerundeDeler",
                column: "DorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicerundeDeler_ServicerundeId",
                table: "ServicerundeDeler",
                column: "ServicerundeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicerundeSjekkpunkter_FullfortAvBrukerId",
                table: "ServicerundeSjekkpunkter",
                column: "FullfortAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicerundeSjekkpunkter_ServicerundeId",
                table: "ServicerundeSjekkpunkter",
                column: "ServicerundeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServicerundeDeler");

            migrationBuilder.DropTable(
                name: "ServicerundeSjekklistepunkter");

            migrationBuilder.DropTable(
                name: "ServicerundeSjekkpunkter");
        }
    }
}
