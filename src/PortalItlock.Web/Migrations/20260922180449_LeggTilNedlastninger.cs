using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilNedlastninger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NedlastningsKategorier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NedlastningsKategorier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NedlastningsFiler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KategoriId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NedlastningsFiler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NedlastningsFiler_NedlastningsKategorier_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "NedlastningsKategorier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NedlastningsKategorier",
                columns: new[] { "Id", "Navn", "Rekkefolge" },
                values: new object[,]
                {
                    { 1, "Salto Space", 1 },
                    { 2, "Salto KS", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NedlastningsFiler_KategoriId",
                table: "NedlastningsFiler",
                column: "KategoriId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NedlastningsFiler");

            migrationBuilder.DropTable(
                name: "NedlastningsKategorier");
        }
    }
}
