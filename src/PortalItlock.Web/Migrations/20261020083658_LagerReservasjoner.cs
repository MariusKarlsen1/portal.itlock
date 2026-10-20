using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LagerReservasjoner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LagerReservasjoner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LagerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: true),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Venter = table.Column<int>(type: "INTEGER", nullable: false),
                    Opprettet = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SistEndret = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpprettetAv = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LagerReservasjoner", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LagerReservasjoner_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LagerReservasjoner_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LagerReservasjoner_Lagre_LagerId",
                        column: x => x.LagerId,
                        principalTable: "Lagre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LagerReservasjoner_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LagerReservasjoner_ArbeidsordreId",
                table: "LagerReservasjoner",
                column: "ArbeidsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_LagerReservasjoner_ComponentId",
                table: "LagerReservasjoner",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_LagerReservasjoner_LagerId",
                table: "LagerReservasjoner",
                column: "LagerId");

            migrationBuilder.CreateIndex(
                name: "IX_LagerReservasjoner_ProsjektId",
                table: "LagerReservasjoner",
                column: "ProsjektId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LagerReservasjoner");
        }
    }
}
