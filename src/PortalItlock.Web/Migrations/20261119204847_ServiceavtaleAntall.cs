using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServiceavtaleAntall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ServiceErTilbud",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ServiceTilbudValg",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceavtaleLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    ErTillegg = table.Column<bool>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: false),
                    StandardPris = table.Column<decimal>(type: "TEXT", nullable: true),
                    Antall = table.Column<decimal>(type: "TEXT", nullable: false),
                    TilleggType = table.Column<string>(type: "TEXT", nullable: true),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceavtaleLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceavtaleLinjer_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrisKategorier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrisKategorier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTilleggMaler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: true),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTilleggMaler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceavtaleLinjer_ProsjektId",
                table: "ServiceavtaleLinjer",
                column: "ProsjektId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceavtaleLinjer");

            migrationBuilder.DropTable(
                name: "ServicePrisKategorier");

            migrationBuilder.DropTable(
                name: "ServiceTilleggMaler");

            migrationBuilder.DropColumn(
                name: "ServiceErTilbud",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceTilbudValg",
                table: "Prosjekter");
        }
    }
}
