using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalItlock.Web.Migrations
{
    /// <inheritdoc />
    public partial class TilbudForside : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TilbudForsider",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Navn = table.Column<string>(type: "TEXT", nullable: false),
                    Innhold = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TilbudForsider", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TilbudForsider",
                columns: new[] { "Id", "Innhold", "Navn" },
                values: new object[] { 1, "# Tilbudsforutsetninger og forbehold\nDette tilbudet er utarbeidet på bakgrunn av mottatt grunnlag fra byggherre/entrepenør/arkitekt og vedlagt beslagsliste.\n\nTilbudet inkluderer ikke HEPRO dørbladlesere til sengerommene da jeg regner med kommunen leverer dette.\n\nForutsetter at elektro medtar UPS hvis ikke kan dette leveres og vil da komme tillegg.\n\n## Montering\nMontasje, rigg, drift og kjøring er inkludert i tilbudet.\n\n## Betalingsbetingelser\nNetto pr. 30 dager.\nVarer leveres og faktureres ved oppstart – eller iht. fremdriftsplan.\nMontasje faktureres etter fremdrift – eller iht. kontraktens faktureringsbetingelser.\n\n## Fremdrift og kommunikasjon\nGjeldende fremdriftsplan legges til grunn og anses som førende for fremdrift.\nE-post regnes som skriftlig kommunikasjon dersom annet ikke er avtalt.\n\n## Leveringstid\nNormalt 4–5 uker.\n\n## Tilbudets gyldighet\nTilbudet er gyldig i 30 dager fra tilbudsdato.\n\n## Låssystem\nLåssystem leveres med patenterte nøkler. Antall nøkler leveres iht. oppgitt mengde. Dersom antall ikke er oppgitt, avregnes dette etter at låsplan er godkjent.\nLåsplan omfatter én (1) gangs utarbeidelse av produksjonsgrunnlag. Endringer etter produksjonsgrunnlag belastes etter medgått tid.\nEndring til annet låssystem enn tilbudt kan medføre priskonsekvens.\n\n## Endrings- og regningsarbeid\nEndrings- og/eller regningsarbeid utføres kun ved skriftlig bestilling.\n\n# Beslag\n\n## Automatikker (slagdør)\nFor dører med slagdørautomatikk må det legges inn spikerslag/forsterkning i dør. Dører med overlysfelt må ha spikerslag minimum 100 mm over karm for montering av slagdørautomatikk.\nSlagdørautomatikk forutsettes levert med standard arm/glideskinne på karmside. Spesialarm kan medføre tillegg og avregnes.\nDøråpner skal monteres utenfor dørens slagradius, være godt synlig og plasseres med betjeningshøyde 0,8–1,1 m over gulv. Prosjekteres av RIE.\nDørbladbredde på dører med dørautomatikk må være minimum 750 mm. Ved doble dører der aktivt felt er større enn passivt, tas forbehold om at aktivt dørblad alene dekker nødvendig rømningsbredde.\n\n## CE-godkjenning og servicekrav\nAlle automatikker og tilhørende sikkerhetsutstyr er prosjektert i samsvar med gjeldende regelverk og krav i Maskindirektivet og NS-EN 16005.\nAutomatikker skal CE-godkjennes som del av installasjonen og skal minimum ha årlig service/vedlikehold utført av kompetent personell. Ved manglende service, bortfaller CE sertifisering og det kan påvirke funksjon, sikkerhet og garanti.\n\n## Elektriske sluttstykker / motorlås / solenoidlås\nKarm–dørblad klaring må være korrekt: 3 mm ± 2 mm.\nDørprodusent må hensynta listetrykk, utfresing og plassering av låskasse.\nDørprodusent må melde tilbake dersom andre stolper må benyttes for å tilfredsstille krav/godkjenninger.\nStolpe til el. sluttstykke er tilfeldig valgt – eventuell endring avregnes.\nDersom adgangskontroll benytter balansert tilbakemelding, kan det kreves sluttstykker med mikrobryter (STEP) – endring avregnes.\n\n## Panikkbeslag\nDører med panikkbeslag må være utført slik at begge dørblader kan åpnes samtidig ved rømning – ansvar dørprodusent.\nMontering forutsetter ferdig dørmiljø og gulv.\n\n# Dørprodusent og elektro\n\n## Dørleverandør medtar (Ld i beslagliste)\nHengsler, låskasser uten mikrobryter, mekaniske sluttstykker, innfelte skåter, karmoverføring med rørføring og trekkertråd ferdig montert, samt skyvedørsautomatikker komplett.\nAlle FG-godkjente dører leveres med godkjent bakkantsikring.\nDørblad/karm skal være forboret/gjenget/forsterket for tilbudte produkter (F i beslagliste).\nDørprodusent må melde tilbake dersom prosjektert stolpe til elektrisk sluttstykke må endres.\n\n## Elektroleverandør/RIE medtar (Le i beslagliste)\nAlle beslag merket Le i beslagliste.\nAll kabling, rørføringer, bokser, strømforsyninger med batteribackup, tilkobling til brannvarslingsanlegg.\n230V driftsspenning og sentral UPS medtas av elektro. Dersom ikke annet er presisert, leveres elektriske lås som 24VDC.\nDet må være minimum 16 mm klaring mellom karm og veggutsparing for plass til kabler i dørmiljø.\nFerdige føringsveier forutsettes slik at synlig kabel ikke forekommer.\n\n# Garanti\n\n## Garantivilkår\nUtstyr skal vedlikeholdes iht. instruks i FDV. Vedlikehold skal utføres av kompetent/opplært personell og kunne dokumenteres.\nDokumentasjon skal vise: hva, når og hvem. Manglende dokumentasjon/feil vedlikehold medfører bortfall av garanti og reklamasjonsrett.", "Standard - Lås & beslag" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TilbudForsider");
        }
    }
}
