namespace PortalItlock.Web.Models;

// Innstillingene for hele plattformen, satt i adminportalen under
// Innstillinger. Én rad. Nøkler lagres kryptert (se HemmelighetService), og
// vises aldri i klartekst etter at de er lagret.
public class PlattformInnstillinger
{
    public int Id { get; set; }

    // Leverandøren (deg), brukt på tilbud, vilkår, prisliste og fakturagrunnlag.
    public string Firmanavn { get; set; } = "itlock AS";
    public string? OrgNr { get; set; }
    public string? Adresse { get; set; }
    public string? Postnr { get; set; }
    public string? Sted { get; set; }
    public string? Telefon { get; set; }
    public string? Epost { get; set; }
    public string? Nettside { get; set; }
    public string? Kontonummer { get; set; }
    public string? Kontaktperson { get; set; }
    public string PortalAdresse { get; set; } = "https://full-kontroll.no";

    // Tripletex for Full Kontroll (og consumer-tokenet som alle kunder bruker).
    public string? TripletexConsumerTokenKryptert { get; set; }
    public string? TripletexEmployeeTokenKryptert { get; set; }
    public bool TripletexTest { get; set; }
    public string? TripletexFirma { get; set; }
    public DateTime? TripletexTestet { get; set; }

    // E-post via Resend.
    public string? ResendApiKeyKryptert { get; set; }
    public string? StandardAvsender { get; set; }
    // Ikke i bruk lenger: organisasjoner med egen adresse sender alltid fra
    // den (full-kontroll.no er godkjent hos Resend). Beholdt i databasen.
    public bool EpostPerOrganisasjon { get; set; }

    // Volumrabatt på lisenser, og rabatt ved årlig forskudd.
    public int Rabatt1FraAntall { get; set; } = 25;
    public int Rabatt1Prosent { get; set; } = 10;
    public int Rabatt2FraAntall { get; set; } = 50;
    public int Rabatt2Prosent { get; set; } = 15;
    public int ArligGratisManeder { get; set; } = 2;
    // Av = ingen volumrabatt / ingen gratis måneder, uten å miste tallene.
    public bool VolumrabattAktiv { get; set; } = true;
    public bool ArligRabattAktiv { get; set; } = true;

    // Gratis måneder som faktisk gis ved årlig forskudd.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int GratisManeder => ArligRabattAktiv ? Math.Max(0, ArligGratisManeder) : 0;

    // Vilkår og databehandleravtale, egne tekster med versjon.
    public string VilkarVersjon { get; set; } = "1.0";
    public DateTime VilkarGjelderFra { get; set; } = new(2026, 11, 1);
    public string? VilkarTekst { get; set; }
    public string? DatabehandlerTekst { get; set; }
    public string? TilbudIntro { get; set; }

    public DateTime SistEndret { get; set; } = DateTime.Now;
}

// Pakker (Start, Pro, Komplett). Lisensprisen per rolle kan avhenge av pakken.
public class PrisPakke
{
    public int Id { get; set; }
    public required string Nokkel { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    // Hva som er med, én linje per punkt.
    public string? Innhold { get; set; }
    public int Rekkefolge { get; set; }
}

public class PakkePris
{
    public int Id { get; set; }
    public required string PakkeNokkel { get; set; }
    public required string ProduktNokkel { get; set; }
    public decimal Kr { get; set; }
}
