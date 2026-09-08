using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilPackageDorFunksjoner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageDorFunksjoner",
                columns: table => new
                {
                    DorFunksjonerId = table.Column<int>(type: "INTEGER", nullable: false),
                    PakkerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDorFunksjoner", x => new { x.DorFunksjonerId, x.PakkerId });
                    table.ForeignKey(
                        name: "FK_PackageDorFunksjoner_DorFunksjoner_DorFunksjonerId",
                        column: x => x.DorFunksjonerId,
                        principalTable: "DorFunksjoner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageDorFunksjoner_Packages_PakkerId",
                        column: x => x.PakkerId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDorFunksjoner_PakkerId",
                table: "PackageDorFunksjoner",
                column: "PakkerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageDorFunksjoner");
        }
    }
}
