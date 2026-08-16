using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class BefaringModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Befaringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Kundenr = table.Column<string>(type: "TEXT", nullable: true),
                    Kundenavn = table.Column<string>(type: "TEXT", nullable: true),
                    Bygg = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    Postnr = table.Column<string>(type: "TEXT", nullable: true),
                    Sted = table.Column<string>(type: "TEXT", nullable: true),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    Tlf = table.Column<string>(type: "TEXT", nullable: true),
                    Epost = table.Column<string>(type: "TEXT", nullable: true),
                    Dato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SystemNr = table.Column<string>(type: "TEXT", nullable: true),
                    BefartAv = table.Column<string>(type: "TEXT", nullable: true),
                    Oppdrag = table.Column<string>(type: "TEXT", nullable: true),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Befaringer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BefaringDorfelt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BefaringId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dornr = table.Column<string>(type: "TEXT", nullable: true),
                    Dornavn = table.Column<string>(type: "TEXT", nullable: true),
                    Dortype = table.Column<string>(type: "TEXT", nullable: true),
                    Floyer = table.Column<string>(type: "TEXT", nullable: true),
                    BxH = table.Column<string>(type: "TEXT", nullable: true),
                    Slagretning = table.Column<string>(type: "TEXT", nullable: true),
                    Lassystemnr = table.Column<string>(type: "TEXT", nullable: true),
                    Brannkrav = table.Column<string>(type: "TEXT", nullable: true),
                    Brannklasse = table.Column<string>(type: "TEXT", nullable: true),
                    Fg = table.Column<string>(type: "TEXT", nullable: true),
                    Sikringsklasse = table.Column<string>(type: "TEXT", nullable: true),
                    Risikoklasse = table.Column<string>(type: "TEXT", nullable: true),
                    UniversellUtforming = table.Column<string>(type: "TEXT", nullable: true),
                    ApnekraftMaks30N = table.Column<string>(type: "TEXT", nullable: true),
                    Dorlukker = table.Column<string>(type: "TEXT", nullable: true),
                    ArmGlideskinne = table.Column<string>(type: "TEXT", nullable: true),
                    VkPlate = table.Column<string>(type: "TEXT", nullable: true),
                    MontasjeSideDorlukker = table.Column<string>(type: "TEXT", nullable: true),
                    AnnetUtstyrDorlukker = table.Column<string>(type: "TEXT", nullable: true),
                    Automatikk = table.Column<string>(type: "TEXT", nullable: true),
                    TrekkSkyvArm = table.Column<string>(type: "TEXT", nullable: true),
                    Adapter = table.Column<string>(type: "TEXT", nullable: true),
                    MontasjeSideAutomatikk = table.Column<string>(type: "TEXT", nullable: true),
                    Albuekontakter = table.Column<string>(type: "TEXT", nullable: true),
                    RadarSensor = table.Column<string>(type: "TEXT", nullable: true),
                    KabelAutomatikk = table.Column<string>(type: "TEXT", nullable: true),
                    UpsNodstrom = table.Column<string>(type: "TEXT", nullable: true),
                    Sikkerhetssensor = table.Column<string>(type: "TEXT", nullable: true),
                    Magnetlas = table.Column<string>(type: "TEXT", nullable: true),
                    BrakettMl = table.Column<string>(type: "TEXT", nullable: true),
                    Panikkbeslag = table.Column<string>(type: "TEXT", nullable: true),
                    Handtak = table.Column<string>(type: "TEXT", nullable: true),
                    AnnetUtstyrOvrig = table.Column<string>(type: "TEXT", nullable: true),
                    Notater = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BefaringDorfelt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BefaringDorfelt_Befaringer_BefaringId",
                        column: x => x.BefaringId,
                        principalTable: "Befaringer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BefaringLassystemer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BefaringDorfeltId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Laskasse = table.Column<string>(type: "TEXT", nullable: true),
                    MekSluttstykke = table.Column<string>(type: "TEXT", nullable: true),
                    Mikrobryter = table.Column<string>(type: "TEXT", nullable: true),
                    ElSluttstykke = table.Column<string>(type: "TEXT", nullable: true),
                    Stolpe = table.Column<string>(type: "TEXT", nullable: true),
                    Volt = table.Column<string>(type: "TEXT", nullable: true),
                    Karmoverforing = table.Column<string>(type: "TEXT", nullable: true),
                    Festelepper = table.Column<string>(type: "TEXT", nullable: true),
                    Kabel = table.Column<string>(type: "TEXT", nullable: true),
                    Dorvrider = table.Column<string>(type: "TEXT", nullable: true),
                    Skilt = table.Column<string>(type: "TEXT", nullable: true),
                    Overflate = table.Column<string>(type: "TEXT", nullable: true),
                    Sylinder = table.Column<string>(type: "TEXT", nullable: true),
                    DortykkelseAB = table.Column<string>(type: "TEXT", nullable: true),
                    Magnetkontakt = table.Column<string>(type: "TEXT", nullable: true),
                    Nodutstyr = table.Column<string>(type: "TEXT", nullable: true),
                    AnnetUtstyr = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BefaringLassystemer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BefaringLassystemer_BefaringDorfelt_BefaringDorfeltId",
                        column: x => x.BefaringDorfeltId,
                        principalTable: "BefaringDorfelt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BefaringDorfelt_BefaringId",
                table: "BefaringDorfelt",
                column: "BefaringId");

            migrationBuilder.CreateIndex(
                name: "IX_BefaringLassystemer_BefaringDorfeltId",
                table: "BefaringLassystemer",
                column: "BefaringDorfeltId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BefaringLassystemer");

            migrationBuilder.DropTable(
                name: "BefaringDorfelt");

            migrationBuilder.DropTable(
                name: "Befaringer");
        }
    }
}
