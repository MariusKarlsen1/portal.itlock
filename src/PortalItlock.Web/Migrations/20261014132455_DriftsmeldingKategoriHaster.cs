using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class DriftsmeldingKategoriHaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Haster",
                table: "Driftsmeldinger",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Kategori",
                table: "Driftsmeldinger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LestDato",
                table: "Driftsmeldinger",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Haster",
                table: "Driftsmeldinger");

            migrationBuilder.DropColumn(
                name: "Kategori",
                table: "Driftsmeldinger");

            migrationBuilder.DropColumn(
                name: "LestDato",
                table: "Driftsmeldinger");
        }
    }
}
