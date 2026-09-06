using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilForesporselMediaOgTilbudBeskrivelse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ForesporselBeskrivelse",
                table: "Tilbud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ForesporselMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ForesporselId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForesporselMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForesporselMedia_Foresporsler_ForesporselId",
                        column: x => x.ForesporselId,
                        principalTable: "Foresporsler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForesporselMedia_ForesporselId",
                table: "ForesporselMedia",
                column: "ForesporselId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForesporselMedia");

            migrationBuilder.DropColumn(
                name: "ForesporselBeskrivelse",
                table: "Tilbud");
        }
    }
}
