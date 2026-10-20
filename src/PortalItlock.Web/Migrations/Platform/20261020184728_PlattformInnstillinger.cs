using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class PlattformInnstillinger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TripletexEmployeeTokenKryptert",
                table: "TenantInnstillinger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripletexFirma",
                table: "TenantInnstillinger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TripletexTestet",
                table: "TenantInnstillinger",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pakke",
                table: "TenantAbonnementer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PakkeStorrelse",
                table: "PrisProdukter",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PakkePriser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PakkeNokkel = table.Column<string>(type: "TEXT", nullable: false),
                    ProduktNokkel = table.Column<string>(type: "TEXT", nullable: false),
                    Kr = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PakkePriser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlattformInnstillinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Firmanavn = table.Column<string>(type: "TEXT", nullable: false),
                    OrgNr = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    Postnr = table.Column<string>(type: "TEXT", nullable: true),
                    Sted = table.Column<string>(type: "TEXT", nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", nullable: true),
                    Epost = table.Column<string>(type: "TEXT", nullable: true),
                    Nettside = table.Column<string>(type: "TEXT", nullable: true),
                    Kontonummer = table.Column<string>(type: "TEXT", nullable: true),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    PortalAdresse = table.Column<string>(type: "TEXT", nullable: false),
                    TripletexConsumerTokenKryptert = table.Column<string>(type: "TEXT", nullable: true),
                    TripletexEmployeeTokenKryptert = table.Column<string>(type: "TEXT", nullable: true),
                    TripletexTest = table.Column<bool>(type: "INTEGER", nullable: false),
                    TripletexFirma = table.Column<string>(type: "TEXT", nullable: true),
                    TripletexTestet = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResendApiKeyKryptert = table.Column<string>(type: "TEXT", nullable: true),
                    StandardAvsender = table.Column<string>(type: "TEXT", nullable: true),
                    EpostPerOrganisasjon = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rabatt1FraAntall = table.Column<int>(type: "INTEGER", nullable: false),
                    Rabatt1Prosent = table.Column<int>(type: "INTEGER", nullable: false),
                    Rabatt2FraAntall = table.Column<int>(type: "INTEGER", nullable: false),
                    Rabatt2Prosent = table.Column<int>(type: "INTEGER", nullable: false),
                    ArligGratisManeder = table.Column<int>(type: "INTEGER", nullable: false),
                    VilkarVersjon = table.Column<string>(type: "TEXT", nullable: false),
                    VilkarGjelderFra = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VilkarTekst = table.Column<string>(type: "TEXT", nullable: true),
                    DatabehandlerTekst = table.Column<string>(type: "TEXT", nullable: true),
                    TilbudIntro = table.Column<string>(type: "TEXT", nullable: true),
                    SistEndret = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlattformInnstillinger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrisPakker",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nokkel = table.Column<string>(type: "TEXT", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Innhold = table.Column<string>(type: "TEXT", nullable: true),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrisPakker", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PakkePriser_PakkeNokkel_ProduktNokkel",
                table: "PakkePriser",
                columns: new[] { "PakkeNokkel", "ProduktNokkel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrisPakker_Nokkel",
                table: "PrisPakker",
                column: "Nokkel",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PakkePriser");

            migrationBuilder.DropTable(
                name: "PlattformInnstillinger");

            migrationBuilder.DropTable(
                name: "PrisPakker");

            migrationBuilder.DropColumn(
                name: "TripletexEmployeeTokenKryptert",
                table: "TenantInnstillinger");

            migrationBuilder.DropColumn(
                name: "TripletexFirma",
                table: "TenantInnstillinger");

            migrationBuilder.DropColumn(
                name: "TripletexTestet",
                table: "TenantInnstillinger");

            migrationBuilder.DropColumn(
                name: "Pakke",
                table: "TenantAbonnementer");

            migrationBuilder.DropColumn(
                name: "PakkeStorrelse",
                table: "PrisProdukter");
        }
    }
}
