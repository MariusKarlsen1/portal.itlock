using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KunngjoringReaksjonPamelding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reaksjon",
                table: "KunngjoringLikes",
                type: "TEXT",
                nullable: false,
                defaultValue: "tommel");

            migrationBuilder.CreateTable(
                name: "KunngjoringPameldinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KunngjoringId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Svar = table.Column<int>(type: "INTEGER", nullable: false),
                    OppdatertDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KunngjoringPameldinger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KunngjoringPameldinger_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KunngjoringPameldinger_Kunngjoringer_KunngjoringId",
                        column: x => x.KunngjoringId,
                        principalTable: "Kunngjoringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringPameldinger_BrukerId",
                table: "KunngjoringPameldinger",
                column: "BrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_KunngjoringPameldinger_KunngjoringId_BrukerId",
                table: "KunngjoringPameldinger",
                columns: new[] { "KunngjoringId", "BrukerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KunngjoringPameldinger");

            migrationBuilder.DropColumn(
                name: "Reaksjon",
                table: "KunngjoringLikes");
        }
    }
}
