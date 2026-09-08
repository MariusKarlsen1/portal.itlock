using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilProduktgrupperOgKundeRabatt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProduktgruppeId",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Produktgrupper",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produktgrupper", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KundeRabatter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KundeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProduktgruppeId = table.Column<int>(type: "INTEGER", nullable: false),
                    RabattProsent = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KundeRabatter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KundeRabatter_Kunder_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KundeRabatter_Produktgrupper_ProduktgruppeId",
                        column: x => x.ProduktgruppeId,
                        principalTable: "Produktgrupper",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_ProduktgruppeId",
                table: "Components",
                column: "ProduktgruppeId");

            migrationBuilder.CreateIndex(
                name: "IX_KundeRabatter_KundeId",
                table: "KundeRabatter",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_KundeRabatter_ProduktgruppeId",
                table: "KundeRabatter",
                column: "ProduktgruppeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Components_Produktgrupper_ProduktgruppeId",
                table: "Components",
                column: "ProduktgruppeId",
                principalTable: "Produktgrupper",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_Produktgrupper_ProduktgruppeId",
                table: "Components");

            migrationBuilder.DropTable(
                name: "KundeRabatter");

            migrationBuilder.DropTable(
                name: "Produktgrupper");

            migrationBuilder.DropIndex(
                name: "IX_Components_ProduktgruppeId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "ProduktgruppeId",
                table: "Components");
        }
    }
}
