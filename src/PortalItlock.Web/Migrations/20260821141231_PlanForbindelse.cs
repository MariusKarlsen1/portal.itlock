using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class PlanForbindelse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanForbindelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FraUtstyrId = table.Column<int>(type: "INTEGER", nullable: false),
                    TilUtstyrId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Notat = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanForbindelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanForbindelser_PlanUtstyr_FraUtstyrId",
                        column: x => x.FraUtstyrId,
                        principalTable: "PlanUtstyr",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanForbindelser_PlanUtstyr_TilUtstyrId",
                        column: x => x.TilUtstyrId,
                        principalTable: "PlanUtstyr",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanForbindelser_FraUtstyrId",
                table: "PlanForbindelser",
                column: "FraUtstyrId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanForbindelser_TilUtstyrId",
                table: "PlanForbindelser",
                column: "TilUtstyrId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanForbindelser");
        }
    }
}
