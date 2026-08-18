using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProsjektStatusPersonerInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Info",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProsjektMedlemmer",
                columns: table => new
                {
                    MedlemmerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProsjekterId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektMedlemmer", x => new { x.MedlemmerId, x.ProsjekterId });
                    table.ForeignKey(
                        name: "FK_ProsjektMedlemmer_Brukere_MedlemmerId",
                        column: x => x.MedlemmerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProsjektMedlemmer_Prosjekter_ProsjekterId",
                        column: x => x.ProsjekterId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProsjektMedlemmer_ProsjekterId",
                table: "ProsjektMedlemmer",
                column: "ProsjekterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProsjektMedlemmer");

            migrationBuilder.DropColumn(
                name: "Info",
                table: "Prosjekter");
        }
    }
}
