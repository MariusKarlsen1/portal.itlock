using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class MontasjeOgFelt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Kilometer",
                table: "Timeregistreringer",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ArbeidsordreSjekkpunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false),
                    Fullfort = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArbeidsordreSjekkpunkter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArbeidsordreSjekkpunkter_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SjekklisteMaler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Dortype = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SjekklisteMaler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SjekklistePunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SjekklisteMalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SjekklistePunkter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SjekklistePunkter_SjekklisteMaler_SjekklisteMalId",
                        column: x => x.SjekklisteMalId,
                        principalTable: "SjekklisteMaler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreSjekkpunkter_ArbeidsordreId",
                table: "ArbeidsordreSjekkpunkter",
                column: "ArbeidsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_SjekklistePunkter_SjekklisteMalId",
                table: "SjekklistePunkter",
                column: "SjekklisteMalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArbeidsordreSjekkpunkter");

            migrationBuilder.DropTable(
                name: "SjekklistePunkter");

            migrationBuilder.DropTable(
                name: "SjekklisteMaler");

            migrationBuilder.DropColumn(
                name: "Kilometer",
                table: "Timeregistreringer");
        }
    }
}
