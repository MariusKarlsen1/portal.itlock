using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TimeregistreringGodkjenning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BehandletAvBrukerId",
                table: "Timeregistreringer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BehandletDato",
                table: "Timeregistreringer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Timeregistreringer",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Timeregistreringer_BehandletAvBrukerId",
                table: "Timeregistreringer",
                column: "BehandletAvBrukerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timeregistreringer_Brukere_BehandletAvBrukerId",
                table: "Timeregistreringer",
                column: "BehandletAvBrukerId",
                principalTable: "Brukere",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timeregistreringer_Brukere_BehandletAvBrukerId",
                table: "Timeregistreringer");

            migrationBuilder.DropIndex(
                name: "IX_Timeregistreringer_BehandletAvBrukerId",
                table: "Timeregistreringer");

            migrationBuilder.DropColumn(
                name: "BehandletAvBrukerId",
                table: "Timeregistreringer");

            migrationBuilder.DropColumn(
                name: "BehandletDato",
                table: "Timeregistreringer");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Timeregistreringer");
        }
    }
}
