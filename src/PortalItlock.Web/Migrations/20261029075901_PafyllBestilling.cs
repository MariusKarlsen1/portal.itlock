using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PafyllBestilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PafyllBestillinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BilLagerId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrukerNavn = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Opprettet = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KlarDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PlukketAv = table.Column<string>(type: "TEXT", nullable: true),
                    HentetDato = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PafyllBestillinger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PafyllBestillinger_Lagre_BilLagerId",
                        column: x => x.BilLagerId,
                        principalTable: "Lagre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PafyllLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PafyllBestillingId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Plukket = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PafyllLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PafyllLinjer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PafyllLinjer_PafyllBestillinger_PafyllBestillingId",
                        column: x => x.PafyllBestillingId,
                        principalTable: "PafyllBestillinger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PafyllBestillinger_BilLagerId",
                table: "PafyllBestillinger",
                column: "BilLagerId");

            migrationBuilder.CreateIndex(
                name: "IX_PafyllLinjer_ComponentId",
                table: "PafyllLinjer",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_PafyllLinjer_PafyllBestillingId",
                table: "PafyllLinjer",
                column: "PafyllBestillingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PafyllLinjer");

            migrationBuilder.DropTable(
                name: "PafyllBestillinger");
        }
    }
}
