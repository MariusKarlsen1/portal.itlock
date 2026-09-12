using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilAktivTimeOkt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AktiveTimeOkter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PauseStartTidspunkt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AkkumulertPauseMinutter = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AktiveTimeOkter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AktiveTimeOkter_Brukere_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AktiveTimeOkter_BrukerId",
                table: "AktiveTimeOkter",
                column: "BrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AktiveTimeOkter");
        }
    }
}
