using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class BefaringKomponenttyperOgSjekklister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "KreverFoto",
                table: "ServicerundeDorPunkter",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Standard",
                table: "ServicerundeDorPunkter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DorId",
                table: "ServiceKontrollpunkter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndretDato",
                table: "ServiceKontrollpunkter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "KreverFoto",
                table: "ServiceKontrollpunkter",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Standard",
                table: "ServiceKontrollpunkter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErPlassholder",
                table: "Components",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KreverFoto",
                table: "ServicerundeDorPunkter");

            migrationBuilder.DropColumn(
                name: "Standard",
                table: "ServicerundeDorPunkter");

            migrationBuilder.DropColumn(
                name: "DorId",
                table: "ServiceKontrollpunkter");

            migrationBuilder.DropColumn(
                name: "EndretDato",
                table: "ServiceKontrollpunkter");

            migrationBuilder.DropColumn(
                name: "KreverFoto",
                table: "ServiceKontrollpunkter");

            migrationBuilder.DropColumn(
                name: "Standard",
                table: "ServiceKontrollpunkter");

            migrationBuilder.DropColumn(
                name: "ErPlassholder",
                table: "Components");
        }
    }
}
