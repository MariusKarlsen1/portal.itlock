using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DorIdMalOgDorFelt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brann",
                table: "Dorer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bredde",
                table: "Dorer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DorIdMalId",
                table: "Dorer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DorTil",
                table: "Dorer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FriBredde086",
                table: "Dorer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Hoyde",
                table: "Dorer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lyd",
                table: "Dorer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DorIdMaler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kode = table.Column<string>(type: "TEXT", nullable: false),
                    Brann = table.Column<string>(type: "TEXT", nullable: true),
                    Lyd = table.Column<string>(type: "TEXT", nullable: true),
                    FriBredde086 = table.Column<bool>(type: "INTEGER", nullable: true),
                    Bredde = table.Column<int>(type: "INTEGER", nullable: true),
                    Hoyde = table.Column<int>(type: "INTEGER", nullable: true),
                    Dortype = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DorIdMaler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DorIdMaler_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dorer_DorIdMalId",
                table: "Dorer",
                column: "DorIdMalId");

            migrationBuilder.CreateIndex(
                name: "IX_DorIdMaler_ProsjektId",
                table: "DorIdMaler",
                column: "ProsjektId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dorer_DorIdMaler_DorIdMalId",
                table: "Dorer",
                column: "DorIdMalId",
                principalTable: "DorIdMaler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dorer_DorIdMaler_DorIdMalId",
                table: "Dorer");

            migrationBuilder.DropTable(
                name: "DorIdMaler");

            migrationBuilder.DropIndex(
                name: "IX_Dorer_DorIdMalId",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "Brann",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "Bredde",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "DorIdMalId",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "DorTil",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "FriBredde086",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "Hoyde",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "Lyd",
                table: "Dorer");
        }
    }
}
