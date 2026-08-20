using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ArbeidsordreAdresse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adresse",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Arbeidsordre",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Arbeidsordre",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postnr",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sted",
                table: "Arbeidsordre",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adresse",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "Postnr",
                table: "Arbeidsordre");

            migrationBuilder.DropColumn(
                name: "Sted",
                table: "Arbeidsordre");
        }
    }
}
