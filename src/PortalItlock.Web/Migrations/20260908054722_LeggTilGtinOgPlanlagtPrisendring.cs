using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilGtinOgPlanlagtPrisendring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gtin",
                table: "Components",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanlagtePrisendringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    GammelPrisNetto = table.Column<decimal>(type: "TEXT", nullable: true),
                    GammelPrisVeiledende = table.Column<decimal>(type: "TEXT", nullable: true),
                    NyPrisNetto = table.Column<decimal>(type: "TEXT", nullable: true),
                    NyPrisVeiledende = table.Column<decimal>(type: "TEXT", nullable: true),
                    GjelderFraDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Utfort = table.Column<bool>(type: "INTEGER", nullable: false),
                    Kilde = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanlagtePrisendringer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanlagtePrisendringer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanlagtePrisendringer_ComponentId",
                table: "PlanlagtePrisendringer",
                column: "ComponentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanlagtePrisendringer");

            migrationBuilder.DropColumn(
                name: "Gtin",
                table: "Components");
        }
    }
}
