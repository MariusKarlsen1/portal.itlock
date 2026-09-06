using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilForesporsel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foresporsler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FraEpost = table.Column<string>(type: "TEXT", nullable: false),
                    FraNavn = table.Column<string>(type: "TEXT", nullable: true),
                    Emne = table.Column<string>(type: "TEXT", nullable: false),
                    Innhold = table.Column<string>(type: "TEXT", nullable: false),
                    MottattDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Lest = table.Column<bool>(type: "INTEGER", nullable: false),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foresporsler", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Foresporsler");
        }
    }
}
