using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PunkterSomEgenFigurtype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KoblingsPunkter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KoblingsPunkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KoblingsSymbolId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelX = table.Column<double>(type: "REAL", nullable: false),
                    RelY = table.Column<double>(type: "REAL", nullable: false),
                    Storrelse = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KoblingsPunkter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KoblingsPunkter_KoblingsSymboler_KoblingsSymbolId",
                        column: x => x.KoblingsSymbolId,
                        principalTable: "KoblingsSymboler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsPunkter_KoblingsSymbolId",
                table: "KoblingsPunkter",
                column: "KoblingsSymbolId");
        }
    }
}
