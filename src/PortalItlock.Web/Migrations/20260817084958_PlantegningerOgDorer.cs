using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PlantegningerOgDorer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plantegninger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NokkelsystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Byggetrinn = table.Column<string>(type: "TEXT", nullable: true),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plantegninger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plantegninger_Nokkelsystemer_NokkelsystemId",
                        column: x => x.NokkelsystemId,
                        principalTable: "Nokkelsystemer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dorer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantegningId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dornummer = table.Column<string>(type: "TEXT", nullable: false),
                    PosX = table.Column<double>(type: "REAL", nullable: false),
                    PosY = table.Column<double>(type: "REAL", nullable: false),
                    Etasje = table.Column<string>(type: "TEXT", nullable: true),
                    Sone = table.Column<string>(type: "TEXT", nullable: true),
                    Dortype = table.Column<string>(type: "TEXT", nullable: true),
                    BxH = table.Column<string>(type: "TEXT", nullable: true),
                    Slagretning = table.Column<string>(type: "TEXT", nullable: true),
                    Notater = table.Column<string>(type: "TEXT", nullable: true),
                    FerdigMontert = table.Column<bool>(type: "INTEGER", nullable: false),
                    MontertDato = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dorer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dorer_Plantegninger_PlantegningId",
                        column: x => x.PlantegningId,
                        principalTable: "Plantegninger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DorKomponenter",
                columns: table => new
                {
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DorKomponenter", x => new { x.DorId, x.ComponentId });
                    table.ForeignKey(
                        name: "FK_DorKomponenter_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DorKomponenter_Dorer_DorId",
                        column: x => x.DorId,
                        principalTable: "Dorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dorer_PlantegningId",
                table: "Dorer",
                column: "PlantegningId");

            migrationBuilder.CreateIndex(
                name: "IX_DorKomponenter_ComponentId",
                table: "DorKomponenter",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_Plantegninger_NokkelsystemId",
                table: "Plantegninger",
                column: "NokkelsystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DorKomponenter");

            migrationBuilder.DropTable(
                name: "Dorer");

            migrationBuilder.DropTable(
                name: "Plantegninger");
        }
    }
}
