using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServiceavtaleOgArbeidsordreTid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PlanlagtSlutt",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Servicerunder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UtfortAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    StatusBeskrivelse = table.Column<string>(type: "TEXT", nullable: false),
                    Anbefalinger = table.Column<string>(type: "TEXT", nullable: true),
                    NesteServiceDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicerunder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicerunder_Brukere_UtfortAvBrukerId",
                        column: x => x.UtfortAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Servicerunder_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Servicerunder_ProsjektId",
                table: "Servicerunder",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicerunder_UtfortAvBrukerId",
                table: "Servicerunder",
                column: "UtfortAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Servicerunder");

            migrationBuilder.DropColumn(
                name: "PlanlagtSlutt",
                table: "Arbeidsordre");
        }
    }
}
