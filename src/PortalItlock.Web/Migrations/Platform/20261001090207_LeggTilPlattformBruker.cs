using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class LeggTilPlattformBruker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlattformBrukere",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Epost = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlattformBrukere", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlattformBrukerPasswordResetTokener",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlattformBrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    UtlopsDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Brukt = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlattformBrukerPasswordResetTokener", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlattformBrukerPasswordResetTokener_PlattformBrukere_PlattformBrukerId",
                        column: x => x.PlattformBrukerId,
                        principalTable: "PlattformBrukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlattformBrukerPasswordResetTokener_PlattformBrukerId",
                table: "PlattformBrukerPasswordResetTokener",
                column: "PlattformBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlattformBrukerPasswordResetTokener");

            migrationBuilder.DropTable(
                name: "PlattformBrukere");
        }
    }
}
