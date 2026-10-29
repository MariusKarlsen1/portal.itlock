using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class BimModell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dorer_ProsjektId",
                table: "Dorer");

            migrationBuilder.AddColumn<string>(
                name: "ModellFilnavn",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModellOpplastet",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModellOpplastetAv",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ModellStorrelse",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IfcGuid",
                table: "Dorer",
                type: "TEXT",
                nullable: true);


            migrationBuilder.CreateIndex(
                name: "IX_Dorer_ProsjektId_IfcGuid",
                table: "Dorer",
                columns: new[] { "ProsjektId", "IfcGuid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dorer_ProsjektId_IfcGuid",
                table: "Dorer");

            migrationBuilder.DropColumn(
                name: "ModellFilnavn",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ModellOpplastet",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ModellOpplastetAv",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ModellStorrelse",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "IfcGuid",
                table: "Dorer");


            migrationBuilder.CreateIndex(
                name: "IX_Dorer_ProsjektId",
                table: "Dorer",
                column: "ProsjektId");
        }
    }
}
