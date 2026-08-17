using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PrisoverslagModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prisoverslag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Kunde = table.Column<string>(type: "TEXT", nullable: true),
                    AntallTimer = table.Column<decimal>(type: "TEXT", nullable: false),
                    Timepris = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaslagProsent = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notater = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prisoverslag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrisoverslagLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrisoverslagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    PrisNetto = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrisVeiledende = table.Column<decimal>(type: "TEXT", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrisoverslagLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrisoverslagLinjer_Prisoverslag_PrisoverslagId",
                        column: x => x.PrisoverslagId,
                        principalTable: "Prisoverslag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrisoverslagLinjer_PrisoverslagId",
                table: "PrisoverslagLinjer",
                column: "PrisoverslagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrisoverslagLinjer");

            migrationBuilder.DropTable(
                name: "Prisoverslag");
        }
    }
}
