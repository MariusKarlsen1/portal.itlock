using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Rabattgruppe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RabattgruppeId",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rabattgrupper",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kode = table.Column<string>(type: "TEXT", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Leverandor = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    RabattProsent = table.Column<decimal>(type: "TEXT", nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rabattgrupper", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_RabattgruppeId",
                table: "Components",
                column: "RabattgruppeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Components_Rabattgrupper_RabattgruppeId",
                table: "Components",
                column: "RabattgruppeId",
                principalTable: "Rabattgrupper",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_Rabattgrupper_RabattgruppeId",
                table: "Components");

            migrationBuilder.DropTable(
                name: "Rabattgrupper");

            migrationBuilder.DropIndex(
                name: "IX_Components_RabattgruppeId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "RabattgruppeId",
                table: "Components");
        }
    }
}
