using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class TenantInnstillinger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantInnstillinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    EpostNavn = table.Column<string>(type: "TEXT", nullable: true),
                    AvsenderNavn = table.Column<string>(type: "TEXT", nullable: true),
                    SvarTilPortal = table.Column<bool>(type: "INTEGER", nullable: false),
                    SvarTilEpost = table.Column<string>(type: "TEXT", nullable: true),
                    SistOkonomiVarsel = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SistEndret = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInnstillinger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInnstillinger_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantVarselMottakere",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Epost = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Service = table.Column<bool>(type: "INTEGER", nullable: false),
                    Lisens = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ticket = table.Column<bool>(type: "INTEGER", nullable: false),
                    Prosjektokonomi = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantVarselMottakere", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantVarselMottakere_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInnstillinger_EpostNavn",
                table: "TenantInnstillinger",
                column: "EpostNavn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantInnstillinger_TenantId",
                table: "TenantInnstillinger",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantVarselMottakere_TenantId",
                table: "TenantVarselMottakere",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantInnstillinger");

            migrationBuilder.DropTable(
                name: "TenantVarselMottakere");
        }
    }
}
