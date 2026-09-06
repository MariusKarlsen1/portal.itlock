using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilNyheter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SisteNyhetSettId",
                table: "Brukere",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Nyheter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tittel = table.Column<string>(type: "TEXT", nullable: false),
                    Innhold = table.Column<string>(type: "TEXT", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nyheter", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nyheter");

            migrationBuilder.DropColumn(
                name: "SisteNyhetSettId",
                table: "Brukere");
        }
    }
}
