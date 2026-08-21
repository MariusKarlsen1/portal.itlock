using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PrisHistorikk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrisHistorikk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    GammelPrisNetto = table.Column<decimal>(type: "TEXT", nullable: true),
                    NyPrisNetto = table.Column<decimal>(type: "TEXT", nullable: true),
                    GammelPrisVeiledende = table.Column<decimal>(type: "TEXT", nullable: true),
                    NyPrisVeiledende = table.Column<decimal>(type: "TEXT", nullable: true),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Kilde = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrisHistorikk", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrisHistorikk_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrisHistorikk_ComponentId",
                table: "PrisHistorikk",
                column: "ComponentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrisHistorikk");
        }
    }
}
