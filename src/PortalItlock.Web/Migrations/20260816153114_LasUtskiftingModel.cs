using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LasUtskiftingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LasUtskiftinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalMm = table.Column<string>(type: "TEXT", nullable: true),
                    UtsideMm = table.Column<string>(type: "TEXT", nullable: true),
                    InnsideMm = table.Column<string>(type: "TEXT", nullable: true),
                    AvstandDorkantTilSkiltMm = table.Column<string>(type: "TEXT", nullable: true),
                    DorFabrikat = table.Column<string>(type: "TEXT", nullable: true),
                    DorNummer = table.Column<string>(type: "TEXT", nullable: true),
                    Stolpehoyde = table.Column<string>(type: "TEXT", nullable: true),
                    Stolpebredde = table.Column<string>(type: "TEXT", nullable: true),
                    Overflatebehandling = table.Column<string>(type: "TEXT", nullable: true),
                    OverflatebehandlingAnnet = table.Column<string>(type: "TEXT", nullable: true),
                    KarmFabrikat = table.Column<string>(type: "TEXT", nullable: true),
                    KarmNummer = table.Column<string>(type: "TEXT", nullable: true),
                    KarmAvstand = table.Column<string>(type: "TEXT", nullable: true),
                    Skruer = table.Column<string>(type: "TEXT", nullable: true),
                    HengslingSide = table.Column<string>(type: "TEXT", nullable: true),
                    Slagretning = table.Column<string>(type: "TEXT", nullable: true),
                    Merknad = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LasUtskiftinger", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LasUtskiftinger");
        }
    }
}
