using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilFelterPaFravarSoknad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HeleDagen",
                table: "FravarSoknader",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "SluttTid",
                table: "FravarSoknader",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTid",
                table: "FravarSoknader",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TilOppfolgingHosBrukerId",
                table: "FravarSoknader",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FravarSoknader_TilOppfolgingHosBrukerId",
                table: "FravarSoknader",
                column: "TilOppfolgingHosBrukerId");

            migrationBuilder.AddForeignKey(
                name: "FK_FravarSoknader_Brukere_TilOppfolgingHosBrukerId",
                table: "FravarSoknader",
                column: "TilOppfolgingHosBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FravarSoknader_Brukere_TilOppfolgingHosBrukerId",
                table: "FravarSoknader");

            migrationBuilder.DropIndex(
                name: "IX_FravarSoknader_TilOppfolgingHosBrukerId",
                table: "FravarSoknader");

            migrationBuilder.DropColumn(
                name: "HeleDagen",
                table: "FravarSoknader");

            migrationBuilder.DropColumn(
                name: "SluttTid",
                table: "FravarSoknader");

            migrationBuilder.DropColumn(
                name: "StartTid",
                table: "FravarSoknader");

            migrationBuilder.DropColumn(
                name: "TilOppfolgingHosBrukerId",
                table: "FravarSoknader");
        }
    }
}
