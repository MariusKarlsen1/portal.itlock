namespace PortalItlock.Web.Models;

public class CeGodkjenning
{
    public int Id { get; set; }
    public int DorId { get; set; }
    public Dor? Dor { get; set; }

    public required string Sertifiseringsnummer { get; set; }
    public CeGodkjenningStatus Status { get; set; } = CeGodkjenningStatus.UnderArbeid;

    public DateTime GyldigFra { get; set; } = DateTime.Now;
    public DateTime GyldigTil { get; set; } = DateTime.Now.AddYears(1);

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
    public int? OpprettetAvBrukerId { get; set; }
    public Bruker? OpprettetAvBruker { get; set; }
    public DateTime? OppdatertDato { get; set; }

    // Steg 1: Client info (forhåndsfylt fra prosjekt/kunde/dør, overstyrbart)
    public string? KundeNavn { get; set; }
    public string? Kontaktperson { get; set; }
    public string? ProsjektNavn { get; set; }
    public string? Adresse { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Etasje { get; set; }
    public string? Bygg { get; set; }
    public string? Byggkategori { get; set; }
    public string? Risikoklasse { get; set; }
    public string? DorTil { get; set; }
    public string? Dornummer { get; set; }
    public string? DorIdKode { get; set; }
    public string? EnergiKlasse { get; set; }
    public int GyldighetMåneder { get; set; } = 12;
    public string? Arbeidsordre { get; set; }
    public bool? Serviceavtale { get; set; }
    public string? Serviceadresse { get; set; }

    // Steg 2: Machine details (forhåndsfylt fra komponenten med CeKategori = DorAutomatikk)
    public string? Produsent { get; set; }
    public string? ProdusentAdresse { get; set; }
    public string? ProdusentPostnr { get; set; }
    public string? ProdusentSted { get; set; }
    public string? ProdusentLand { get; set; }
    public string? ProdusentOrgnr { get; set; }
    public string? ItemNavn { get; set; }
    public string? Serienummer { get; set; }
    public int? Antall { get; set; }
    public int? ProduksjonsAar { get; set; }

    // Steg 3: Door design (forhåndsfylt fra Dor/DorIdMal der feltet finnes)
    public int? BreddeMm { get; set; }
    public int? HoydeMm { get; set; }
    public double? VektKg { get; set; }
    public string? Dorkonstruksjon { get; set; }
    public string? Karmkonstruksjon { get; set; }
    public bool? GlassIDor { get; set; }
    public bool? GlassSynligTiltak { get; set; }
    public bool? GlassFareKuttSkade { get; set; }
    public bool? FriBredde086 { get; set; }
    public bool? TerskelUnder25mm { get; set; }
    public string? Brannklasse { get; set; }
    public bool? KuttskadeRisiko { get; set; }
    public string? FargeKarm { get; set; }
    public string? FargeDorblad { get; set; }
    public string? Karmtype { get; set; }
    public string? Terskeltype { get; set; }
    public string? Sparkeplate { get; set; }
    public int? AMal { get; set; }
    public int? BMal { get; set; }
    public string? Dorblad { get; set; }
    public string? Glasstykkelse { get; set; }

    // Steg 4: Measurements
    public double? Apningsvinkel { get; set; }
    public double? ApningstidSek { get; set; }
    public bool ApningstidUnntatt { get; set; }
    public double? LukketidHoySek { get; set; }
    public bool LukketidHoyUnntatt { get; set; }
    public double? LukketidLavSek { get; set; }
    public bool LukketidLavUnntatt { get; set; }
    public double? ApningskraftN { get; set; }
    public bool ApningskraftUnntatt { get; set; }
    public bool? DodlasEtterStopp { get; set; }
    public bool? ForsinkelseForLukking { get; set; }
    public double? AvstandTrappCm { get; set; }
    public bool AvstandTrappUnntatt { get; set; }
    public double? AvstandVeggCm { get; set; }
    public bool AvstandVeggUnntatt { get; set; }
    public bool? ApnesMotGjennomgangstrafikk { get; set; }
    public string? MalKommentar { get; set; }

    // Steg 5: Control
    public bool? SensorplasseringKorrekt { get; set; }
    public bool? ReaksjonstidOk { get; set; }
    public bool? SikkerhetssensorUtkoblingBrannalarm { get; set; }
    public bool? NodapningTestet { get; set; }
    public bool? ImpulsbryterKorrektHoyde { get; set; }
    public bool? AktiveringsbryterFriPlass { get; set; }
    public bool? TydeligSkilting { get; set; }
    public bool? HengselsideBeskyttet { get; set; }
    public bool? ElektroniskLasKoblingTestet { get; set; }
    public bool? EkstraFunksjonerTestet { get; set; }

    // Steg 6: Function test
    public bool FotograferingIkkeTillatt { get; set; }

    // Steg 7: Signing
    public string? QrKodeSkann { get; set; }
    public string? UtfortAvNavn { get; set; }
    public DateTime? UtfortAvDato { get; set; }
    public byte[]? UtfortAvSignatur { get; set; }
    public string? VerifisertAvNavn { get; set; }
    public DateTime? VerifisertAvDato { get; set; }
    public byte[]? VerifisertAvSignatur { get; set; }

    public List<CeGodkjenningMedia> Media { get; set; } = [];
}
