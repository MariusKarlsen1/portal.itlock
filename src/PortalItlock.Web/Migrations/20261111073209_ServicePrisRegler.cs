using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class ServicePrisRegler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFastTillegg",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceGodkjentArspris",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceGodkjentAv",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceGodkjentDato",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceGodkjentGrunnlag",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceMinstepris",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceOppmote",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceRabattProsent",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceReise",
                table: "Prosjekter",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Servicetype",
                table: "ComponentTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServicePrisRegler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Prioritet = table.Column<int>(type: "INTEGER", nullable: false),
                    KreverAlle = table.Column<bool>(type: "INTEGER", nullable: false),
                    Pris = table.Column<decimal>(type: "TEXT", nullable: false),
                    Minutter = table.Column<int>(type: "INTEGER", nullable: false),
                    Undertekst = table.Column<string>(type: "TEXT", nullable: true),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpprettetDato = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndretDato = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrisRegler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrisRegelBetingelser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServicePrisRegelId = table.Column<int>(type: "INTEGER", nullable: false),
                    Felt = table.Column<string>(type: "TEXT", nullable: false),
                    Verdi = table.Column<string>(type: "TEXT", nullable: false),
                    ErIkke = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rekkefolge = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrisRegelBetingelser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePrisRegelBetingelser_ServicePrisRegler_ServicePrisRegelId",
                        column: x => x.ServicePrisRegelId,
                        principalTable: "ServicePrisRegler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 9,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 11,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 12,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 13,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 14,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 15,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 16,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 17,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 18,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 19,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 20,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 21,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 22,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 23,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 24,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 25,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 26,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 27,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 28,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 29,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 30,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 31,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 32,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 33,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 34,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 35,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 36,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 37,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 38,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 39,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 40,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 41,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 42,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 43,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 44,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 45,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 46,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 47,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 48,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 49,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 50,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 51,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 52,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 53,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 54,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 55,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 56,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 57,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 58,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 59,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 60,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 61,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 62,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 63,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 64,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "ComponentTypes",
                keyColumn: "Id",
                keyValue: 65,
                column: "Servicetype",
                value: null);

            migrationBuilder.UpdateData(
                table: "TilbudForsider",
                keyColumn: "Id",
                keyValue: 1,
                column: "Innhold",
                value: "# Tilbudsforutsetninger og forbehold\r\nDette tilbudet er utarbeidet på bakgrunn av mottatt grunnlag fra byggherre/entrepenør/arkitekt og vedlagt beslagsliste.\r\n\r\nTilbudet inkluderer ikke HEPRO dørbladlesere til sengerommene da jeg regner med kommunen leverer dette.\r\n\r\nForutsetter at elektro medtar UPS hvis ikke kan dette leveres og vil da komme tillegg.\r\n\r\n## Montering\r\nMontasje, rigg, drift og kjøring er inkludert i tilbudet.\r\n\r\n## Betalingsbetingelser\r\nNetto pr. 30 dager.\r\nVarer leveres og faktureres ved oppstart – eller iht. fremdriftsplan.\r\nMontasje faktureres etter fremdrift – eller iht. kontraktens faktureringsbetingelser.\r\n\r\n## Fremdrift og kommunikasjon\r\nGjeldende fremdriftsplan legges til grunn og anses som førende for fremdrift.\r\nE-post regnes som skriftlig kommunikasjon dersom annet ikke er avtalt.\r\n\r\n## Leveringstid\r\nNormalt 4–5 uker.\r\n\r\n## Tilbudets gyldighet\r\nTilbudet er gyldig i 30 dager fra tilbudsdato.\r\n\r\n## Låssystem\r\nLåssystem leveres med patenterte nøkler. Antall nøkler leveres iht. oppgitt mengde. Dersom antall ikke er oppgitt, avregnes dette etter at låsplan er godkjent.\r\nLåsplan omfatter én (1) gangs utarbeidelse av produksjonsgrunnlag. Endringer etter produksjonsgrunnlag belastes etter medgått tid.\r\nEndring til annet låssystem enn tilbudt kan medføre priskonsekvens.\r\n\r\n## Endrings- og regningsarbeid\r\nEndrings- og/eller regningsarbeid utføres kun ved skriftlig bestilling.\r\n\r\n# Beslag\r\n\r\n## Automatikker (slagdør)\r\nFor dører med slagdørautomatikk må det legges inn spikerslag/forsterkning i dør. Dører med overlysfelt må ha spikerslag minimum 100 mm over karm for montering av slagdørautomatikk.\r\nSlagdørautomatikk forutsettes levert med standard arm/glideskinne på karmside. Spesialarm kan medføre tillegg og avregnes.\r\nDøråpner skal monteres utenfor dørens slagradius, være godt synlig og plasseres med betjeningshøyde 0,8–1,1 m over gulv. Prosjekteres av RIE.\r\nDørbladbredde på dører med dørautomatikk må være minimum 750 mm. Ved doble dører der aktivt felt er større enn passivt, tas forbehold om at aktivt dørblad alene dekker nødvendig rømningsbredde.\r\n\r\n## CE-godkjenning og servicekrav\r\nAlle automatikker og tilhørende sikkerhetsutstyr er prosjektert i samsvar med gjeldende regelverk og krav i Maskindirektivet og NS-EN 16005.\r\nAutomatikker skal CE-godkjennes som del av installasjonen og skal minimum ha årlig service/vedlikehold utført av kompetent personell. Ved manglende service, bortfaller CE sertifisering og det kan påvirke funksjon, sikkerhet og garanti.\r\n\r\n## Elektriske sluttstykker / motorlås / solenoidlås\r\nKarm–dørblad klaring må være korrekt: 3 mm ± 2 mm.\r\nDørprodusent må hensynta listetrykk, utfresing og plassering av låskasse.\r\nDørprodusent må melde tilbake dersom andre stolper må benyttes for å tilfredsstille krav/godkjenninger.\r\nStolpe til el. sluttstykke er tilfeldig valgt – eventuell endring avregnes.\r\nDersom adgangskontroll benytter balansert tilbakemelding, kan det kreves sluttstykker med mikrobryter (STEP) – endring avregnes.\r\n\r\n## Panikkbeslag\r\nDører med panikkbeslag må være utført slik at begge dørblader kan åpnes samtidig ved rømning – ansvar dørprodusent.\r\nMontering forutsetter ferdig dørmiljø og gulv.\r\n\r\n# Dørprodusent og elektro\r\n\r\n## Dørleverandør medtar (Ld i beslagliste)\r\nHengsler, låskasser uten mikrobryter, mekaniske sluttstykker, innfelte skåter, karmoverføring med rørføring og trekkertråd ferdig montert, samt skyvedørsautomatikker komplett.\r\nAlle FG-godkjente dører leveres med godkjent bakkantsikring.\r\nDørblad/karm skal være forboret/gjenget/forsterket for tilbudte produkter (F i beslagliste).\r\nDørprodusent må melde tilbake dersom prosjektert stolpe til elektrisk sluttstykke må endres.\r\n\r\n## Elektroleverandør/RIE medtar (Le i beslagliste)\r\nAlle beslag merket Le i beslagliste.\r\nAll kabling, rørføringer, bokser, strømforsyninger med batteribackup, tilkobling til brannvarslingsanlegg.\r\n230V driftsspenning og sentral UPS medtas av elektro. Dersom ikke annet er presisert, leveres elektriske lås som 24VDC.\r\nDet må være minimum 16 mm klaring mellom karm og veggutsparing for plass til kabler i dørmiljø.\r\nFerdige føringsveier forutsettes slik at synlig kabel ikke forekommer.\r\n\r\n# Garanti\r\n\r\n## Garantivilkår\r\nUtstyr skal vedlikeholdes iht. instruks i FDV. Vedlikehold skal utføres av kompetent/opplært personell og kunne dokumenteres.\r\nDokumentasjon skal vise: hva, når og hvem. Manglende dokumentasjon/feil vedlikehold medfører bortfall av garanti og reklamasjonsrett.");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrisRegelBetingelser_ServicePrisRegelId",
                table: "ServicePrisRegelBetingelser",
                column: "ServicePrisRegelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServicePrisRegelBetingelser");

            migrationBuilder.DropTable(
                name: "ServicePrisRegler");

            migrationBuilder.DropColumn(
                name: "ServiceFastTillegg",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceGodkjentArspris",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceGodkjentAv",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceGodkjentDato",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceGodkjentGrunnlag",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceMinstepris",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceOppmote",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceRabattProsent",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "ServiceReise",
                table: "Prosjekter");

            migrationBuilder.DropColumn(
                name: "Servicetype",
                table: "ComponentTypes");

            migrationBuilder.UpdateData(
                table: "TilbudForsider",
                keyColumn: "Id",
                keyValue: 1,
                column: "Innhold",
                value: "# Tilbudsforutsetninger og forbehold\nDette tilbudet er utarbeidet på bakgrunn av mottatt grunnlag fra byggherre/entrepenør/arkitekt og vedlagt beslagsliste.\n\nTilbudet inkluderer ikke HEPRO dørbladlesere til sengerommene da jeg regner med kommunen leverer dette.\n\nForutsetter at elektro medtar UPS hvis ikke kan dette leveres og vil da komme tillegg.\n\n## Montering\nMontasje, rigg, drift og kjøring er inkludert i tilbudet.\n\n## Betalingsbetingelser\nNetto pr. 30 dager.\nVarer leveres og faktureres ved oppstart – eller iht. fremdriftsplan.\nMontasje faktureres etter fremdrift – eller iht. kontraktens faktureringsbetingelser.\n\n## Fremdrift og kommunikasjon\nGjeldende fremdriftsplan legges til grunn og anses som førende for fremdrift.\nE-post regnes som skriftlig kommunikasjon dersom annet ikke er avtalt.\n\n## Leveringstid\nNormalt 4–5 uker.\n\n## Tilbudets gyldighet\nTilbudet er gyldig i 30 dager fra tilbudsdato.\n\n## Låssystem\nLåssystem leveres med patenterte nøkler. Antall nøkler leveres iht. oppgitt mengde. Dersom antall ikke er oppgitt, avregnes dette etter at låsplan er godkjent.\nLåsplan omfatter én (1) gangs utarbeidelse av produksjonsgrunnlag. Endringer etter produksjonsgrunnlag belastes etter medgått tid.\nEndring til annet låssystem enn tilbudt kan medføre priskonsekvens.\n\n## Endrings- og regningsarbeid\nEndrings- og/eller regningsarbeid utføres kun ved skriftlig bestilling.\n\n# Beslag\n\n## Automatikker (slagdør)\nFor dører med slagdørautomatikk må det legges inn spikerslag/forsterkning i dør. Dører med overlysfelt må ha spikerslag minimum 100 mm over karm for montering av slagdørautomatikk.\nSlagdørautomatikk forutsettes levert med standard arm/glideskinne på karmside. Spesialarm kan medføre tillegg og avregnes.\nDøråpner skal monteres utenfor dørens slagradius, være godt synlig og plasseres med betjeningshøyde 0,8–1,1 m over gulv. Prosjekteres av RIE.\nDørbladbredde på dører med dørautomatikk må være minimum 750 mm. Ved doble dører der aktivt felt er større enn passivt, tas forbehold om at aktivt dørblad alene dekker nødvendig rømningsbredde.\n\n## CE-godkjenning og servicekrav\nAlle automatikker og tilhørende sikkerhetsutstyr er prosjektert i samsvar med gjeldende regelverk og krav i Maskindirektivet og NS-EN 16005.\nAutomatikker skal CE-godkjennes som del av installasjonen og skal minimum ha årlig service/vedlikehold utført av kompetent personell. Ved manglende service, bortfaller CE sertifisering og det kan påvirke funksjon, sikkerhet og garanti.\n\n## Elektriske sluttstykker / motorlås / solenoidlås\nKarm–dørblad klaring må være korrekt: 3 mm ± 2 mm.\nDørprodusent må hensynta listetrykk, utfresing og plassering av låskasse.\nDørprodusent må melde tilbake dersom andre stolper må benyttes for å tilfredsstille krav/godkjenninger.\nStolpe til el. sluttstykke er tilfeldig valgt – eventuell endring avregnes.\nDersom adgangskontroll benytter balansert tilbakemelding, kan det kreves sluttstykker med mikrobryter (STEP) – endring avregnes.\n\n## Panikkbeslag\nDører med panikkbeslag må være utført slik at begge dørblader kan åpnes samtidig ved rømning – ansvar dørprodusent.\nMontering forutsetter ferdig dørmiljø og gulv.\n\n# Dørprodusent og elektro\n\n## Dørleverandør medtar (Ld i beslagliste)\nHengsler, låskasser uten mikrobryter, mekaniske sluttstykker, innfelte skåter, karmoverføring med rørføring og trekkertråd ferdig montert, samt skyvedørsautomatikker komplett.\nAlle FG-godkjente dører leveres med godkjent bakkantsikring.\nDørblad/karm skal være forboret/gjenget/forsterket for tilbudte produkter (F i beslagliste).\nDørprodusent må melde tilbake dersom prosjektert stolpe til elektrisk sluttstykke må endres.\n\n## Elektroleverandør/RIE medtar (Le i beslagliste)\nAlle beslag merket Le i beslagliste.\nAll kabling, rørføringer, bokser, strømforsyninger med batteribackup, tilkobling til brannvarslingsanlegg.\n230V driftsspenning og sentral UPS medtas av elektro. Dersom ikke annet er presisert, leveres elektriske lås som 24VDC.\nDet må være minimum 16 mm klaring mellom karm og veggutsparing for plass til kabler i dørmiljø.\nFerdige føringsveier forutsettes slik at synlig kabel ikke forekommer.\n\n# Garanti\n\n## Garantivilkår\nUtstyr skal vedlikeholdes iht. instruks i FDV. Vedlikehold skal utføres av kompetent/opplært personell og kunne dokumenteres.\nDokumentasjon skal vise: hva, når og hvem. Manglende dokumentasjon/feil vedlikehold medfører bortfall av garanti og reklamasjonsrett.");
        }
    }
}
