using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilImporthistorikk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Importhistorikk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kilde = table.Column<string>(type: "TEXT", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: true),
                    Leverandor = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    AntallRader = table.Column<int>(type: "INTEGER", nullable: true),
                    AntallNye = table.Column<int>(type: "INTEGER", nullable: true),
                    AntallOppdatert = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Importhistorikk", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Importhistorikk_Brukere_OpprettetAvBrukerId",
                        column: x => x.OpprettetAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Importhistorikk_OpprettetAvBrukerId",
                table: "Importhistorikk",
                column: "OpprettetAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Importhistorikk");
        }
    }
}
