using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProsjektByggetrinn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProsjektByggetrinn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsjektByggetrinn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsjektByggetrinn_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateIndex(
                name: "IX_ProsjektByggetrinn_ProsjektId",
                table: "ProsjektByggetrinn",
                column: "ProsjektId");

            // Byggetrinnene som allerede står på tegningene, i alfabetisk rekkefølge per prosjekt.
            migrationBuilder.Sql(@"
                INSERT INTO ProsjektByggetrinn (ProsjektId, Navn, Rekkefolge)
                SELECT ProsjektId, Navn, (ROW_NUMBER() OVER (PARTITION BY ProsjektId ORDER BY Navn)) - 1
                FROM (SELECT DISTINCT ProsjektId, TRIM(Byggetrinn) AS Navn
                      FROM Plantegninger
                      WHERE Byggetrinn IS NOT NULL AND TRIM(Byggetrinn) <> '');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProsjektByggetrinn");

        }
    }
}
