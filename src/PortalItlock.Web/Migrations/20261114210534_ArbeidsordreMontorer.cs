using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ArbeidsordreMontorer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArbeidsordreMontorer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArbeidsordreMontorer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArbeidsordreMontorer_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArbeidsordreMontorer_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreMontorer_ArbeidsordreId_BrukerId",
                table: "ArbeidsordreMontorer",
                columns: new[] { "ArbeidsordreId", "BrukerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreMontorer_BrukerId",
                table: "ArbeidsordreMontorer",
                column: "BrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArbeidsordreMontorer");
        }
    }
}
