using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LasplanReserveSylindere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LasplanReserver",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Notat = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LasplanReserver", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LasplanReserver_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LasplanReserver_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NokkelLasplanReserver",
                columns: table => new
                {
                    NokkelId = table.Column<int>(type: "INTEGER", nullable: false),
                    LasplanReserveId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NokkelLasplanReserver", x => new { x.NokkelId, x.LasplanReserveId });
                    table.ForeignKey(
                        name: "FK_NokkelLasplanReserver_LasplanReserver_LasplanReserveId",
                        column: x => x.LasplanReserveId,
                        principalTable: "LasplanReserver",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NokkelLasplanReserver_Nokler_NokkelId",
                        column: x => x.NokkelId,
                        principalTable: "Nokler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LasplanReserver_ComponentId",
                table: "LasplanReserver",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_LasplanReserver_ProsjektId",
                table: "LasplanReserver",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_NokkelLasplanReserver_LasplanReserveId",
                table: "NokkelLasplanReserver",
                column: "LasplanReserveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NokkelLasplanReserver");

            migrationBuilder.DropTable(
                name: "LasplanReserver");
        }
    }
}
