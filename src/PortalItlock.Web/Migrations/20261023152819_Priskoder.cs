using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Priskoder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BrukerKodelinjer",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisKodelinjerIPdf",
                table: "Tilbud",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PrisKoder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kode = table.Column<string>(type: "TEXT", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Enhet = table.Column<int>(type: "INTEGER", nullable: false),
                    Kostpris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Salgspris = table.Column<decimal>(type: "TEXT", nullable: false),
                    ErMontasje = table.Column<bool>(type: "INTEGER", nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrisKoder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TilbudKodeLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TilbudId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrisKodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Kode = table.Column<string>(type: "TEXT", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Enhet = table.Column<int>(type: "INTEGER", nullable: false),
                    Antall = table.Column<decimal>(type: "TEXT", nullable: false),
                    Kostpris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Salgspris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false),
                    FraMontasjetid = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilbudKodeLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TilbudKodeLinjer_Tilbud_TilbudId",
                        column: x => x.TilbudId,
                        principalTable: "Tilbud",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrisKoder_Kode",
                table: "PrisKoder",
                column: "Kode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TilbudKodeLinjer_TilbudId",
                table: "TilbudKodeLinjer",
                column: "TilbudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrisKoder");

            migrationBuilder.DropTable(
                name: "TilbudKodeLinjer");

            migrationBuilder.DropColumn(
                name: "BrukerKodelinjer",
                table: "Tilbud");

            migrationBuilder.DropColumn(
                name: "VisKodelinjerIPdf",
                table: "Tilbud");
        }
    }
}
