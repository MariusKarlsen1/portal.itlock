using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServicerapportSignatur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "KundeSignatur",
                table: "Servicerunder",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KundeSignertAv",
                table: "Servicerunder",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KundeSignertDato",
                table: "Servicerunder",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KundeSignatur",
                table: "Servicerunder");

            migrationBuilder.DropColumn(
                name: "KundeSignertAv",
                table: "Servicerunder");

            migrationBuilder.DropColumn(
                name: "KundeSignertDato",
                table: "Servicerunder");
        }
    }
}
