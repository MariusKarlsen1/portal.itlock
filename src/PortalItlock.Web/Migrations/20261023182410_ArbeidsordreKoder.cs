using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ArbeidsordreKoder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrisKodeId",
                table: "Timeregistreringer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AndreProsentKost",
                table: "TilbudBudsjetter",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ArbeidsordreKodeLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrisKodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Kode = table.Column<string>(type: "TEXT", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Enhet = table.Column<int>(type: "INTEGER", nullable: false),
                    Antall = table.Column<decimal>(type: "TEXT", nullable: false),
                    Kostpris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Salgspris = table.Column<decimal>(type: "TEXT", nullable: false),
                    LagtTilAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    LagtTilDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArbeidsordreKodeLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArbeidsordreKodeLinjer_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreKodeLinjer_ArbeidsordreId",
                table: "ArbeidsordreKodeLinjer",
                column: "ArbeidsordreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArbeidsordreKodeLinjer");

            migrationBuilder.DropColumn(
                name: "PrisKodeId",
                table: "Timeregistreringer");

            migrationBuilder.DropColumn(
                name: "AndreProsentKost",
                table: "TilbudBudsjetter");
        }
    }
}
