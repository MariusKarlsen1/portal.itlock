using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilGarantitidOgOverlevertDato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GarantiUtlopsdato",
                table: "Dorer");

            migrationBuilder.AddColumn<DateTime>(
                name: "OverlevertDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GarantitidManeder",
                table: "Components",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverlevertDato",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "GarantitidManeder",
                table: "Components");

            migrationBuilder.AddColumn<DateTime>(
                name: "GarantiUtlopsdato",
                table: "Dorer",
                type: "TEXT",
                nullable: true);
        }
    }
}
