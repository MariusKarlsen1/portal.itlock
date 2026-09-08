using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilArbeidsordreVareOgTimepris : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KostprisTime",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Timepris",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArbeidsordreVarer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArbeidsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Kostpris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Utpris = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArbeidsordreVarer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArbeidsordreVarer_Arbeidsordre_ArbeidsordreId",
                        column: x => x.ArbeidsordreId,
                        principalTable: "Arbeidsordre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArbeidsordreVarer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreVarer_ArbeidsordreId",
                table: "ArbeidsordreVarer",
                column: "ArbeidsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreVarer_ComponentId",
                table: "ArbeidsordreVarer",
                column: "ComponentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArbeidsordreVarer");

            migrationBuilder.DropColumn(
                name: "KostprisTime",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "Timepris",
                table: "Arbeidsordre");
        }
    }
}
