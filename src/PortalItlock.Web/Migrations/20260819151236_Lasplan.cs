using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Lasplan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LasplanLast",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LasplanProsjektnummer",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LasplanSystemnr",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LasplanUtarbeidetAv",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErSylinder",
                table: "Components",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Nokler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Merking = table.Column<string>(type: "TEXT", nullable: true),
                    Materiale = table.Column<string>(type: "TEXT", nullable: true),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nokler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nokler_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NokkelSylindere",
                columns: table => new
                {
                    NokkelId = table.Column<int>(type: "INTEGER", nullable: false),
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NokkelSylindere", x => new { x.NokkelId, x.DorId, x.ComponentId });
                    table.ForeignKey(
                        name: "FK_NokkelSylindere_Nokler_NokkelId",
                        column: x => x.NokkelId,
                        principalTable: "Nokler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nokler_ProsjektId",
                table: "Nokler",
                column: "ProsjektId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NokkelSylindere");

            migrationBuilder.DropTable(
                name: "Nokler");

            migrationBuilder.DropColumn(
                name: "LasplanLast",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanProsjektnummer",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanSystemnr",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanUtarbeidetAv",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ErSylinder",
                table: "Components");
        }
    }
}
