using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilBrukerPasswordResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrukerPasswordResetTokener",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    UtlopsDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Brukt = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrukerPasswordResetTokener", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrukerPasswordResetTokener_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrukerPasswordResetTokener_BrukerId",
                table: "BrukerPasswordResetTokener",
                column: "BrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_BrukerPasswordResetTokener_Token",
                table: "BrukerPasswordResetTokener",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrukerPasswordResetTokener");
        }
    }
}
