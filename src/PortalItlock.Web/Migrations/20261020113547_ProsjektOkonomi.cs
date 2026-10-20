using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProsjektOkonomi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OkonomiInnstillinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InternTimekost = table.Column<decimal>(type: "TEXT", nullable: true),
                    GulProsentOverBudsjett = table.Column<int>(type: "INTEGER", nullable: false),
                    GulTimerBruktProsent = table.Column<int>(type: "INTEGER", nullable: false),
                    GulFremdriftProsent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkonomiInnstillinger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProsjektEtterkalkyler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kontrollert = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KontrollertAv = table.Column<string>(type: "TEXT", nullable: true),
                    Arsak = table.Column<int>(type: "INTEGER", nullable: false),
                    Kommentar = table.Column<string>(type: "TEXT", nullable: true),
                    Inntekt = table.Column<decimal>(type: "TEXT", nullable: false),
                    BudsjettTimer = table.Column<decimal>(type: "TEXT", nullable: false),
                    FaktiskTimer = table.Column<decimal>(type: "TEXT", nullable: false),
                    BudsjettVarer = table.Column<decimal>(type: "TEXT", nullable: false),
                    FaktiskVarer = table.Column<decimal>(type: "TEXT", nullable: false),
                    BudsjettKost = table.Column<decimal>(type: "TEXT", nullable: false),
                    FaktiskKost = table.Column<decimal>(type: "TEXT", nullable: false),
                    BudsjettDekningsgrad = table.Column<decimal>(type: "TEXT", nullable: true),
                    FaktiskDekningsgrad = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektEtterkalkyler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektEtterkalkyler_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TilbudBudsjetter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TilbudId = table.Column<int>(type: "INTEGER", nullable: false),
                    Salgssum = table.Column<decimal>(type: "TEXT", nullable: false),
                    Varekost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Timer = table.Column<decimal>(type: "TEXT", nullable: false),
                    Timesats = table.Column<decimal>(type: "TEXT", nullable: false),
                    Laast = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilbudBudsjetter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TilbudBudsjetter_Tilbud_TilbudId",
                        column: x => x.TilbudId,
                        principalTable: "Tilbud",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektEtterkalkyler_ProsjektId",
                table: "ProsjektEtterkalkyler",
                column: "ProsjektId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TilbudBudsjetter_TilbudId",
                table: "TilbudBudsjetter",
                column: "TilbudId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OkonomiInnstillinger");

            migrationBuilder.DropTable(
                name: "ProsjektEtterkalkyler");

            migrationBuilder.DropTable(
                name: "TilbudBudsjetter");
        }
    }
}
