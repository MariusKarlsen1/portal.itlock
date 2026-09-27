using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKunngjoringer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SisteKunngjoringSettId",
                table: "Brukere",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Kunngjoringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    Innhold = table.Column<string>(type: "TEXT", nullable: false),
                    Kategori = table.Column<int>(type: "INTEGER", nullable: false),
                    Festet = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpprettetAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kunngjoringer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kunngjoringer_Brukere_OpprettetAvBrukerId",
                        column: x => x.OpprettetAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kunngjoringer_OpprettetAvBrukerId",
                table: "Kunngjoringer",
                column: "OpprettetAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "SisteKunngjoringSettId",
                table: "Brukere");
        }
    }
}
