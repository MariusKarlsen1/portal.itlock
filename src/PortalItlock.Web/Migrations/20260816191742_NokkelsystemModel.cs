using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class NokkelsystemModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nokkelsystemer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Systemnummer = table.Column<string>(type: "TEXT", nullable: false),
                    Kundenavn = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    Postnr = table.Column<string>(type: "TEXT", nullable: true),
                    Sted = table.Column<string>(type: "TEXT", nullable: true),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", nullable: true),
                    Epost = table.Column<string>(type: "TEXT", nullable: true),
                    Fabrikat = table.Column<string>(type: "TEXT", nullable: true),
                    Notater = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nokkelsystemer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rekvirenter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NokkelsystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", nullable: true),
                    Epost = table.Column<string>(type: "TEXT", nullable: true),
                    Rolle = table.Column<string>(type: "TEXT", nullable: true),
                    Notater = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rekvirenter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rekvirenter_Nokkelsystemer_NokkelsystemId",
                        column: x => x.NokkelsystemId,
                        principalTable: "Nokkelsystemer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rekvirenter_NokkelsystemId",
                table: "Rekvirenter",
                column: "NokkelsystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rekvirenter");

            migrationBuilder.DropTable(
                name: "Nokkelsystemer");
        }
    }
}
