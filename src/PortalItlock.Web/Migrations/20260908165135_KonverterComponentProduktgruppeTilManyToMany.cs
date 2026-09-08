using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KonverterComponentProduktgruppeTilManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComponentProduktgrupper",
                columns: table => new
                {
                    KomponenterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProduktgrupperId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentProduktgrupper", x => new { x.KomponenterId, x.ProduktgrupperId });
                    table.ForeignKey(
                        name: "FK_ComponentProduktgrupper_Components_KomponenterId",
                        column: x => x.KomponenterId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentProduktgrupper_Produktgrupper_ProduktgrupperId",
                        column: x => x.ProduktgrupperId,
                        principalTable: "Produktgrupper",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentProduktgrupper_ProduktgrupperId",
                table: "ComponentProduktgrupper",
                column: "ProduktgrupperId");

            // Bevar eksisterende enkelt-tilknytninger (fra da Component.ProduktgruppeId var en vanlig FK) i den nye koblingstabellen før kolonnen fjernes.
            migrationBuilder.Sql(
                "INSERT INTO \"ComponentProduktgrupper\" (\"KomponenterId\", \"ProduktgrupperId\") " +
                "SELECT \"Id\", \"ProduktgruppeId\" FROM \"Components\" WHERE \"ProduktgruppeId\" IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Components_Produktgrupper_ProduktgruppeId",
                table: "Components");

            migrationBuilder.DropIndex(
                name: "IX_Components_ProduktgruppeId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "ProduktgruppeId",
                table: "Components");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentProduktgrupper");

            migrationBuilder.AddColumn<int>(
                name: "ProduktgruppeId",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Components_ProduktgruppeId",
                table: "Components",
                column: "ProduktgruppeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Components_Produktgrupper_ProduktgruppeId",
                table: "Components",
                column: "ProduktgruppeId",
                principalTable: "Produktgrupper",
                principalColumn: "Id");
        }
    }
}
