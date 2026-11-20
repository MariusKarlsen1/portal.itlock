namespace PortalItlock.Web.Models;

// Oppsett → Priser: kategoriene som kommer på fanen Prising på hver ny serviceavtale
// (prismodell «Antall», se ServiceavtaleAntall). Pris = per enhet per servicerunde, eks. mva.
public class ServicePrisKategori
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }
    public decimal Pris { get; set; }
    public int Rekkefolge { get; set; }

    // Hvilke dører kategorien teller når et prosjekt gjøres om til serviceavtale
    // (ServiceavtaleAntall.Telling). Null = gjettes ut fra navnet.
    public string? Type { get; set; }
}

// Oppsett → Tillegg: faste tillegg som fylles inn på fanen Tillegg på hver ny avtale,
// f.eks. kjøring per km. Pris null = fylles inn per avtale.
public class ServiceTilleggMal
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string Type { get; set; } = ServiceavtaleLinje.PerRunde;
    public decimal? Pris { get; set; }
    public int Rekkefolge { get; set; }
}

// En linje på en serviceavtale med prismodell «Antall»: enten en kategori fra Oppsett
// (antall per runde × pris) eller et tillegg (oppmøte, kjøring, påslag …).
// Linjene er en kopi av oppsettet da avtalen ble laget, så senere endringer i
// Oppsett ikke endrer prisen på avtaler som finnes.
public class ServiceavtaleLinje
{
    // Tilleggstyper
    public const string PerRunde = "runde";
    public const string PerKm = "km";
    public const string PerAr = "ar";
    public const string Prosent = "pst";

    public int Id { get; set; }
    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public bool ErTillegg { get; set; }
    public required string Navn { get; set; }
    public string? Beskrivelse { get; set; }

    // Kategori: pris per enhet per runde. Tillegg: kr per runde/km/år eller prosent.
    public decimal Pris { get; set; }
    // Prisen fra Oppsett da linjen ble laget – viser «Endret» og kan settes tilbake.
    public decimal? StandardPris { get; set; }
    // Kategori: antall per runde. Tillegg: antall per runde (runde), km per runde (km)
    // eller antall per år (ar). Brukes ikke for prosent.
    public decimal Antall { get; set; }
    public string? TilleggType { get; set; }
    public int Rekkefolge { get; set; }
}
