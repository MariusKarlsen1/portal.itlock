using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServicehenvendelseEpost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EpostSendtDato",
                table: "Servicehenvendelser",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErDokumentasjon",
                table: "ServicehenvendelseBilder",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EpostSendtDato",
                table: "Servicehenvendelser");

            migrationBuilder.DropColumn(
                name: "ErDokumentasjon",
                table: "ServicehenvendelseBilder");
        }
    }
}
