using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class Tofaktor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TofaktorHemmelighet",
                table: "Brukere",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TofaktorPaDato",
                table: "Brukere",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TofaktorReservekoder",
                table: "Brukere",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TofaktorSisteSteg",
                table: "Brukere",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TofaktorStempel",
                table: "Brukere",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SikkerhetInnstillinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KrevTofaktorRoller = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SikkerhetInnstillinger", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SikkerhetInnstillinger");

            migrationBuilder.DropColumn(
                name: "TofaktorHemmelighet",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "TofaktorPaDato",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "TofaktorReservekoder",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "TofaktorSisteSteg",
                table: "Brukere");

            migrationBuilder.DropColumn(
                name: "TofaktorStempel",
                table: "Brukere");
        }
    }
}
