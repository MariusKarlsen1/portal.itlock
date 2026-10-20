using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LagerInnkjop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InnkjopsordreId",
                table: "LagerBevegelser",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Innkjopsordrer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                    LeverandorId = table.Column<int>(type: "INTEGER", nullable: true),
                    LeverandorNavn = table.Column<string>(type: "TEXT", nullable: false),
                    LeverandorEpost = table.Column<string>(type: "TEXT", nullable: true),
                    LagerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProsjektId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Opprettet = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpprettetAv = table.Column<string>(type: "TEXT", nullable: true),
                    Sendt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SendtTil = table.Column<string>(type: "TEXT", nullable: true),
                    OnsketLevering = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VarReferanse = table.Column<string>(type: "TEXT", nullable: true),
                    Notat = table.Column<string>(type: "TEXT", nullable: true),
                    BekreftetDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LeverandorOrdrenr = table.Column<string>(type: "TEXT", nullable: true),
                    BekreftetAv = table.Column<string>(type: "TEXT", nullable: true),
                    ObFilnavn = table.Column<string>(type: "TEXT", nullable: true),
                    AvvikKommentar = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Innkjopsordrer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Innkjopsordrer_Lagre_LagerId",
                        column: x => x.LagerId,
                        principalTable: "Lagre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Innkjopsordrer_Leverandorer_LeverandorId",
                        column: x => x.LeverandorId,
                        principalTable: "Leverandorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Innkjopsordrer_Prosjekter_ProsjektId",
                        column: x => x.ProsjektId,
                        principalTable: "Prosjekter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InnkjopsordreFiler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InnkjopsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Lastet = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnkjopsordreFiler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InnkjopsordreFiler_Innkjopsordrer_InnkjopsordreId",
                        column: x => x.InnkjopsordreId,
                        principalTable: "Innkjopsordrer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InnkjopsordreLinjer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InnkjopsordreId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Varenummer = table.Column<string>(type: "TEXT", nullable: true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Enhet = table.Column<string>(type: "TEXT", nullable: true),
                    Antall = table.Column<int>(type: "INTEGER", nullable: false),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: false),
                    BekreftetAntall = table.Column<int>(type: "INTEGER", nullable: true),
                    BekreftetPris = table.Column<decimal>(type: "TEXT", nullable: true),
                    BekreftetLevering = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Mottatt = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnkjopsordreLinjer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InnkjopsordreLinjer_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InnkjopsordreLinjer_Innkjopsordrer_InnkjopsordreId",
                        column: x => x.InnkjopsordreId,
                        principalTable: "Innkjopsordrer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LagerBevegelser_InnkjopsordreId",
                table: "LagerBevegelser",
                column: "InnkjopsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_InnkjopsordreFiler_InnkjopsordreId",
                table: "InnkjopsordreFiler",
                column: "InnkjopsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_InnkjopsordreLinjer_ComponentId",
                table: "InnkjopsordreLinjer",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_InnkjopsordreLinjer_InnkjopsordreId",
                table: "InnkjopsordreLinjer",
                column: "InnkjopsordreId");

            migrationBuilder.CreateIndex(
                name: "IX_Innkjopsordrer_LagerId",
                table: "Innkjopsordrer",
                column: "LagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Innkjopsordrer_LeverandorId",
                table: "Innkjopsordrer",
                column: "LeverandorId");

            migrationBuilder.CreateIndex(
                name: "IX_Innkjopsordrer_Nummer",
                table: "Innkjopsordrer",
                column: "Nummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Innkjopsordrer_ProsjektId",
                table: "Innkjopsordrer",
                column: "ProsjektId");

            migrationBuilder.CreateIndex(
                name: "IX_Innkjopsordrer_Status",
                table: "Innkjopsordrer",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InnkjopsordreFiler");

            migrationBuilder.DropTable(
                name: "InnkjopsordreLinjer");

            migrationBuilder.DropTable(
                name: "Innkjopsordrer");

            migrationBuilder.DropIndex(
                name: "IX_LagerBevegelser_InnkjopsordreId",
                table: "LagerBevegelser");

            migrationBuilder.DropColumn(
                name: "InnkjopsordreId",
                table: "LagerBevegelser");
        }
    }
}
