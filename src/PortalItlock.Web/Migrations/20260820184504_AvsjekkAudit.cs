using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class AvsjekkAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MontertAvBrukerId",
                table: "Dorer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FullfortAvBrukerId",
                table: "ArbeidsordreSjekkpunkter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FullfortDato",
                table: "ArbeidsordreSjekkpunkter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dorer_MontertAvBrukerId",
                table: "Dorer",
                column: "MontertAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArbeidsordreSjekkpunkter_FullfortAvBrukerId",
                table: "ArbeidsordreSjekkpunkter",
                column: "FullfortAvBrukerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArbeidsordreSjekkpunkter_Brukere_FullfortAvBrukerId",
                table: "ArbeidsordreSjekkpunkter",
                column: "FullfortAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Dorer_Brukere_MontertAvBrukerId",
                table: "Dorer",
                column: "MontertAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArbeidsordreSjekkpunkter_Brukere_FullfortAvBrukerId",
                table: "ArbeidsordreSjekkpunkter");

            migrationBuilder.DropForeignKey(
                name: "FK_Dorer_Brukere_MontertAvBrukerId",
                table: "Dorer");

            migrationBuilder.DropIndex(
                name: "IX_Dorer_MontertAvBrukerId",
                table: "Dorer");

            migrationBuilder.DropIndex(
                name: "IX_ArbeidsordreSjekkpunkter_FullfortAvBrukerId",
                table: "ArbeidsordreSjekkpunkter");

            migrationBuilder.DropColumn(
                name: "MontertAvBrukerId",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "FullfortAvBrukerId",
                table: "ArbeidsordreSjekkpunkter");

            migrationBuilder.DropColumn(
                name: "FullfortDato",
                table: "ArbeidsordreSjekkpunkter");
        }
    }
}
