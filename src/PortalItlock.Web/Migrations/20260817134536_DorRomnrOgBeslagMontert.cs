using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DorRomnrOgBeslagMontert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Montert",
                table: "DorKomponenter",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MontertDato",
                table: "DorKomponenter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Romnr",
                table: "Dorer",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Montert",
                table: "DorKomponenter");

            migrationBuilder.DropColumn(
                name: "MontertDato",
                table: "DorKomponenter");

            migrationBuilder.DropColumn(
                name: "Romnr",
                table: "Dorer");
        }
    }
}
