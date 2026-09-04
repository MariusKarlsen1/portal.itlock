using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilCeGodkjenning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CeDokumentContentType",
                table: "ComponentTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "CeDokumentData",
                table: "ComponentTypes",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CeDokumentFilnavn",
                table: "ComponentTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CeKategori",
                table: "ComponentTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CeGodkjenninger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sertifiseringsnummer = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    GyldigFra = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GyldigTil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpprettetAvBrukerId = table.Column<int>(type: "INTEGER", nullable: true),
                    OppdatertDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    KundeNavn = table.Column<string>(type: "TEXT", nullable: true),
                    Kontaktperson = table.Column<string>(type: "TEXT", nullable: true),
                    ProsjektNavn = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    Etasje = table.Column<string>(type: "TEXT", nullable: true),
                    Bygg = table.Column<string>(type: "TEXT", nullable: true),
                    Byggkategori = table.Column<string>(type: "TEXT", nullable: true),
                    Risikoklasse = table.Column<string>(type: "TEXT", nullable: true),
                    DorTil = table.Column<string>(type: "TEXT", nullable: true),
                    Dornummer = table.Column<string>(type: "TEXT", nullable: true),
                    Produsent = table.Column<string>(type: "TEXT", nullable: true),
                    ItemNavn = table.Column<string>(type: "TEXT", nullable: true),
                    Antall = table.Column<int>(type: "INTEGER", nullable: true),
                    ProduksjonsAar = table.Column<int>(type: "INTEGER", nullable: true),
                    BreddeMm = table.Column<int>(type: "INTEGER", nullable: true),
                    HoydeMm = table.Column<int>(type: "INTEGER", nullable: true),
                    VektKg = table.Column<double>(type: "REAL", nullable: true),
                    Dorkonstruksjon = table.Column<string>(type: "TEXT", nullable: true),
                    Karmkonstruksjon = table.Column<string>(type: "TEXT", nullable: true),
                    GlassIDor = table.Column<bool>(type: "INTEGER", nullable: true),
                    FriBredde086 = table.Column<bool>(type: "INTEGER", nullable: true),
                    TerskelUnder25mm = table.Column<bool>(type: "INTEGER", nullable: true),
                    Brannklasse = table.Column<string>(type: "TEXT", nullable: true),
                    KuttskadeRisiko = table.Column<bool>(type: "INTEGER", nullable: true),
                    Apningsvinkel = table.Column<double>(type: "REAL", nullable: true),
                    ApningstidSek = table.Column<double>(type: "REAL", nullable: true),
                    LukketidHoySek = table.Column<double>(type: "REAL", nullable: true),
                    LukketidLavSek = table.Column<double>(type: "REAL", nullable: true),
                    ApningskraftN = table.Column<double>(type: "REAL", nullable: true),
                    DodlasEtterStopp = table.Column<bool>(type: "INTEGER", nullable: true),
                    ForsinkelseForLukking = table.Column<bool>(type: "INTEGER", nullable: true),
                    AvstandTrappCm = table.Column<double>(type: "REAL", nullable: true),
                    AvstandTrappUnntatt = table.Column<bool>(type: "INTEGER", nullable: false),
                    AvstandVeggCm = table.Column<double>(type: "REAL", nullable: true),
                    AvstandVeggUnntatt = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApnesMotGjennomgangstrafikk = table.Column<bool>(type: "INTEGER", nullable: true),
                    MalKommentar = table.Column<string>(type: "TEXT", nullable: true),
                    SensorplasseringKorrekt = table.Column<bool>(type: "INTEGER", nullable: true),
                    ReaksjonstidOk = table.Column<bool>(type: "INTEGER", nullable: true),
                    SikkerhetssensorUtkoblingBrannalarm = table.Column<bool>(type: "INTEGER", nullable: true),
                    NodapningTestet = table.Column<bool>(type: "INTEGER", nullable: true),
                    ImpulsbryterKorrektHoyde = table.Column<bool>(type: "INTEGER", nullable: true),
                    AktiveringsbryterFriPlass = table.Column<bool>(type: "INTEGER", nullable: true),
                    TydeligSkilting = table.Column<bool>(type: "INTEGER", nullable: true),
                    HengselsideBeskyttet = table.Column<bool>(type: "INTEGER", nullable: true),
                    ElektroniskLasKoblingTestet = table.Column<bool>(type: "INTEGER", nullable: true),
                    EkstraFunksjonerTestet = table.Column<bool>(type: "INTEGER", nullable: true),
                    FotograferingIkkeTillatt = table.Column<bool>(type: "INTEGER", nullable: false),
                    QrKodeSkann = table.Column<string>(type: "TEXT", nullable: true),
                    UtfortAvNavn = table.Column<string>(type: "TEXT", nullable: true),
                    UtfortAvDato = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VerifisertAvNavn = table.Column<string>(type: "TEXT", nullable: true),
                    VerifisertAvDato = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeGodkjenninger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CeGodkjenninger_Brukere_OpprettetAvBrukerId",
                        column: x => x.OpprettetAvBrukerId,
                        principalTable: "Brukere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CeGodkjenninger_Dorer_DorId",
                        column: x => x.DorId,
                        principalTable: "Dorer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CeMaleGrenseverdier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaksApningstidSek = table.Column<double>(type: "REAL", nullable: false),
                    MaksLukketidHoySek = table.Column<double>(type: "REAL", nullable: false),
                    MaksLukketidLavSek = table.Column<double>(type: "REAL", nullable: false),
                    MaksApningskraftN = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeMaleGrenseverdier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CeGodkjenningMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CeGodkjenningId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Filnavn = table.Column<string>(type: "TEXT", nullable: false),
                    Plassering = table.Column<int>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeGodkjenningMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CeGodkjenningMedia_CeGodkjenninger_CeGodkjenningId",
                        column: x => x.CeGodkjenningId,
                        principalTable: "CeGodkjenninger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 43,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 44,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 45,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 46,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 47,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 48,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 49,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 50,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 53,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 54,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 55,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 56,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 57,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 58,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 59,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 60,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 61,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 62,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 63,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 64,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 65,
                columns: new[] { "CeDokumentContentType", "CeDokumentData", "CeDokumentFilnavn", "CeKategori" },
                values: new object[] { null, null, null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_CeGodkjenninger_DorId",
                table: "CeGodkjenninger",
                column: "DorId");

            migrationBuilder.CreateIndex(
                name: "IX_CeGodkjenninger_OpprettetAvBrukerId",
                table: "CeGodkjenninger",
                column: "OpprettetAvBrukerId");

            migrationBuilder.CreateIndex(
                name: "IX_CeGodkjenningMedia_CeGodkjenningId",
                table: "CeGodkjenningMedia",
                column: "CeGodkjenningId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CeGodkjenningMedia");

            migrationBuilder.DropTable(
                name: "CeMaleGrenseverdier");

            migrationBuilder.DropTable(
                name: "CeGodkjenninger");

            migrationBuilder.DropColumn(
                name: "CeDokumentContentType",
                table: "ComponentTypes");

            migrationBuilder.DropColumn(
                name: "CeDokumentData",
                table: "ComponentTypes");

            migrationBuilder.DropColumn(
                name: "CeDokumentFilnavn",
                table: "ComponentTypes");

            migrationBuilder.DropColumn(
                name: "CeKategori",
                table: "ComponentTypes");
        }
    }
}
