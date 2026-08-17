using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DorFunksjoner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DorFunksjoner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DorFunksjoner", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DorDorFunksjoner",
                columns: table => new
                {
                    DorerId = table.Column<int>(type: "INTEGER", nullable: false),
                    FunksjonerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DorDorFunksjoner", x => new { x.DorerId, x.FunksjonerId });
                    table.ForeignKey(
                        name: "FK_DorDorFunksjoner_DorFunksjoner_FunksjonerId",
                        column: x => x.FunksjonerId,
                        principalTable: "DorFunksjoner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DorDorFunksjoner_Dorer_DorerId",
                        column: x => x.DorerId,
                        principalTable: "Dorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DorDorFunksjoner_FunksjonerId",
                table: "DorDorFunksjoner",
                column: "FunksjonerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DorDorFunksjoner");

            migrationBuilder.DropTable(
                name: "DorFunksjoner");
        }
    }
}
