using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilArrangementFelterPaKunngjoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Detaljer",
                table: "Kunngjoringer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventDato",
                table: "Kunngjoringer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InfoTekst",
                table: "Kunngjoringer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sted",
                table: "Kunngjoringer",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Detaljer",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "EventDato",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "InfoTekst",
                table: "Kunngjoringer");

            migrationBuilder.DropColumn(
                name: "Sted",
                table: "Kunngjoringer");
        }
    }
}
