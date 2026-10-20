using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations.Platform
{
    /// <inheritdoc />
    public partial class PlattformAbonnement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlattformFakturaer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodeFra = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodeTil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Opprettet = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    BelopEksMva = table.Column<decimal>(type: "TEXT", nullable: false),
                    TripletexOrdreId = table.Column<int>(type: "INTEGER", nullable: true),
                    TripletexOrdreNummer = table.Column<string>(type: "TEXT", nullable: true),
                    TripletexFakturaNummer = table.Column<int>(type: "INTEGER", nullable: true),
                    Fakturadato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Forfall = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Utestaende = table.Column<decimal>(type: "TEXT", nullable: true),
                    BetaltDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Feil = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlattformFakturaer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlattformFakturaer_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrisProdukter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nokkel = table.Column<string>(type: "TEXT", nullable: false),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Beskrivelse = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Kr = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrisProdukter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAbonnementer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Startdato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BindingTil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Grunnpakke = table.Column<bool>(type: "INTEGER", nullable: false),
                    Intervall = table.Column<int>(type: "INTEGER", nullable: false),
                    BetalingsfristDager = table.Column<int>(type: "INTEGER", nullable: false),
                    FakturaEpost = table.Column<string>(type: "TEXT", nullable: true),
                    NesteFakturaFra = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EtableringFakturert = table.Column<bool>(type: "INTEGER", nullable: false),
                    Etableringsgebyr = table.Column<decimal>(type: "TEXT", nullable: true),
                    ProveperiodeTil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AvtaleSignert = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DatabehandleravtaleSignert = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OppsagtDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PauseFra = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TripletexKundeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAbonnementer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAbonnementer_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantAntallEndringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nokkel = table.Column<string>(type: "TEXT", nullable: false),
                    FraAntall = table.Column<int>(type: "INTEGER", nullable: false),
                    TilAntall = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Fakturert = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAntallEndringer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAntallEndringer_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantAvtalepriser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nokkel = table.Column<string>(type: "TEXT", nullable: false),
                    Kr = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAvtalepriser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAvtalepriser_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantHendelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tidspunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Ikon = table.Column<string>(type: "TEXT", nullable: false),
                    Av = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantHendelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantHendelser_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantNotater",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Oppfolging = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Utfort = table.Column<bool>(type: "INTEGER", nullable: false),
                    Av = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantNotater", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantNotater_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlattformFakturaLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FakturaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    Antall = table.Column<decimal>(type: "TEXT", nullable: false),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlattformFakturaLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlattformFakturaLinjer_PlattformFakturaer_FakturaId",
                        column: x => x.FakturaId,
                        principalTable: "PlattformFakturaer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlattformFakturaer_TenantId_PeriodeFra",
                table: "PlattformFakturaer",
                columns: new[] { "TenantId", "PeriodeFra" });

            migrationBuilder.CreateIndex(
                name: "IX_PlattformFakturaLinjer_FakturaId",
                table: "PlattformFakturaLinjer",
                column: "FakturaId");

            migrationBuilder.CreateIndex(
                name: "IX_PrisProdukter_Nokkel",
                table: "PrisProdukter",
                column: "Nokkel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAbonnementer_TenantId",
                table: "TenantAbonnementer",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantAntallEndringer_TenantId",
                table: "TenantAntallEndringer",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAvtalepriser_TenantId_Nokkel",
                table: "TenantAvtalepriser",
                columns: new[] { "TenantId", "Nokkel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantHendelser_TenantId",
                table: "TenantHendelser",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantNotater_TenantId",
                table: "TenantNotater",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlattformFakturaLinjer");

            migrationBuilder.DropTable(
                name: "PrisProdukter");

            migrationBuilder.DropTable(
                name: "TenantAbonnementer");

            migrationBuilder.DropTable(
                name: "TenantAntallEndringer");

            migrationBuilder.DropTable(
                name: "TenantAvtalepriser");

            migrationBuilder.DropTable(
                name: "TenantHendelser");

            migrationBuilder.DropTable(
                name: "TenantNotater");

            migrationBuilder.DropTable(
                name: "PlattformFakturaer");
        }
    }
}
