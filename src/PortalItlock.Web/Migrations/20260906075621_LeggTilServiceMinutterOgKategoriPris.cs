using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilServiceMinutterOgKategoriPris : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ServiceTimepris",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceKategoriPriser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kategori = table.Column<string>(type: "TEXT", nullable: false),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: true),
                    KravServicePrAar = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceKategoriPriser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceKategoriPriser_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceMinuttLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Enhet = table.Column<string>(type: "TEXT", nullable: true),
                    Minutter = table.Column<int>(type: "INTEGER", nullable: true),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMinuttLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceMinuttLinjer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceMinuttLinjer_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceKategoriPriser_ProsjektId",
                table: "ServiceKategoriPriser",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMinuttLinjer_ComponentId",
                table: "ServiceMinuttLinjer",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMinuttLinjer_ProsjektId",
                table: "ServiceMinuttLinjer",
                column: "ProsjektId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceKategoriPriser");

            migrationBuilder.DropTable(
                name: "ServiceMinuttLinjer");

            migrationBuilder.DropColumn(
                name: "ServiceTimepris",
                table: "Prosjekter");
        }
    }
}
