using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LasplanGodkjenning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LasplanGodkjentAv",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LasplanGodkjentDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LasplanGodkjentFirma",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LasplanKundeEndringer",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LasplanLogg",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LasplanSendtDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LasplanSendtTil",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LasplanSignatur",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LasplanStatus",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LasplanToken",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LasplanTokenUtloper",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prosjekter_LasplanToken",
                table: "Prosjekter",
                column: "LasplanToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prosjekter_LasplanToken",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanGodkjentAv",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanGodkjentDato",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanGodkjentFirma",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanKundeEndringer",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanLogg",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanSendtDato",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanSendtTil",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanSignatur",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanStatus",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanToken",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "LasplanTokenUtloper",
                table: "Prosjekter");
        }
    }
}
