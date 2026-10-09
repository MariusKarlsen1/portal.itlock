using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKoblingsSkjemaDetaljer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Beskrivelse",
                table: "KoblingsSkjemaer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notater",
                table: "KoblingsSkjemaer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Versjon",
                table: "KoblingsSkjemaer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KoblingsSkjemaKomponenter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KoblingsSkjemaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Ikon = table.Column<string>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KoblingsSkjemaKomponenter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KoblingsSkjemaKomponenter_KoblingsSkjemaer_KoblingsSkjemaId",
                        column: x => x.KoblingsSkjemaId,
                        principalTable: "KoblingsSkjemaer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KoblingsSkjemaKomponenter_KoblingsSkjemaId",
                table: "KoblingsSkjemaKomponenter",
                column: "KoblingsSkjemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KoblingsSkjemaKomponenter");

            migrationBuilder.DropColumn(
                name: "Beskrivelse",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropColumn(
                name: "Notater",
                table: "KoblingsSkjemaer");

            migrationBuilder.DropColumn(
                name: "Versjon",
                table: "KoblingsSkjemaer");
        }
    }
}
