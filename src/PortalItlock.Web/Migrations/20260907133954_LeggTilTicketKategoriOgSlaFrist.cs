using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTicketKategoriOgSlaFrist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sla",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "Sluttoppsummering",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaFrist",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TicketKategorier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Gruppe = table.Column<int>(type: "INTEGER", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketKategorier", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TicketKategorier",
                columns: new[] { "Id", "Gruppe", "Navn", "Rekkefolge" },
                values: new object[,]
                {
                    { 1, 0, "Service", 1 },
                    { 2, 0, "Bestilling", 2 },
                    { 3, 0, "Prosjekt", 3 },
                    { 4, 1, "Adgangskontroll", 1 },
                    { 5, 1, "Lås", 2 },
                    { 6, 1, "Kamera", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketKategorier");

            migrationBuilder.DropColumn(
                name: "SlaFrist",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Sluttoppsummering",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "Sla",
                table: "Tickets",
                type: "TEXT",
                nullable: true);
        }
    }
}
