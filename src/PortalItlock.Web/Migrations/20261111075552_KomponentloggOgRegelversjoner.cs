using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class KomponentloggOgRegelversjoner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceGodkjentRegelversjon",
                table: "Prosjekter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DorKomponentLogger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Produktkode = table.Column<string>(type: "TEXT", nullable: true),
                    Komponenttype = table.Column<string>(type: "TEXT", nullable: true),
                    Hendelse = table.Column<string>(type: "TEXT", nullable: false),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Merknad = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DorKomponentLogger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrisRegelVersjoner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Versjon = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AvNavn = table.Column<string>(type: "TEXT", nullable: true),
                    AntallRegler = table.Column<int>(type: "INTEGER", nullable: false),
                    Innhold = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrisRegelVersjoner", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DorKomponentLogger_DorId",
                table: "DorKomponentLogger",
                column: "DorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrisRegelVersjoner_Versjon",
                table: "ServicePrisRegelVersjoner",
                column: "Versjon",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DorKomponentLogger");

            migrationBuilder.DropTable(
                name: "ServicePrisRegelVersjoner");

            migrationBuilder.DropColumn(
                name: "ServiceGodkjentRegelversjon",
                table: "Prosjekter");
        }
    }
}
