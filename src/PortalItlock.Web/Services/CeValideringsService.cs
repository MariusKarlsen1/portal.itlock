using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public record CeValideringsResultat(string Tekst, bool Bestatt);

// Regelmotor for steg 9 (Validation) i CE-godkjenningen. Reglene er et tilpasset
// sett basert på felt portalen faktisk sporer (dørdesign, mål, kontrollsvar,
// funksjonstest og utfyllingsgrad) - ikke en juridisk fasit, men en praktisk
// fullstendighets-/rimelighetssjekk før godkjenningen sendes til gjennomgang.
//
// De enkelte Er*Ok-metodene brukes både her og direkte i CE-veiviseren
// (for å farge et felt gult med en gang et svar vil gjøre at godkjenningen
// feiler), slik at "gult i steget" og "feilet i Validation" alltid er
// nøyaktig samme regel - aldri to steder som kan komme i utakt.
public static class CeValideringsService
{
    public static List<CeValideringsResultat> Evaluer(CeGodkjenning ce, List<DorKomponent> dorKomponenter)
    {
        var sikkerhetsTilbehor = dorKomponenter
            .Where(k => k.Component?.Type?.CeKategori == CeTilbehorKategori.SikkerhetsTilbehor)
            .ToList();

        return
        [
            new("Kuttskaderisiko i dørdesign er vurdert", ce.KuttskadeRisiko.HasValue),
            new("Ingen kjent kuttskaderisiko i dørmiljø", ErKuttskaderisikoOk(ce)),
            new("Glass i dør er trygt (synlig tiltak, ingen kuttfare)", ErGlassOk(ce)),
            new("Dørkonstruksjon og karmkonstruksjon er dokumentert",
                !string.IsNullOrWhiteSpace(ce.Dorkonstruksjon) && !string.IsNullOrWhiteSpace(ce.Karmkonstruksjon)),
            new("Fri passasjebredde oppfyller minstekravet (≥0,86m)", ErFriBreddeOk(ce)),
            new("Terskelhøyde oppfyller kravet (<25mm)", ErTerskelOk(ce)),
            new("Dødgang etter stopp og lukkeforsinkelse oppfyller kravet", ErDodlasOk(ce) && ErForsinkelseOk(ce)),
            new("Hengselside er beskyttet", ErHengselsideOk(ce)),
            new("Sensorer er installert og programmert korrekt",
                sikkerhetsTilbehor.Count > 0 && ErSensorplasseringOk(ce) && ErReaksjonstidOk(ce)),
            new("Sikkerhetssensor frigjøres ved brannalarm", ErBrannalarmUtkoblingOk(ce)),
            new("Aktiveringsenheter er korrekt installert", ErAktiveringsbryterOk(ce) && ErImpulsbryterOk(ce)),
            new("Elektronisk låskobling testet", ErLaskoblingOk(ce)),
            new("Klar merking for alle brukergrupper", ErSkiltingOk(ce)),
            new("Funksjonstest-media er levert", ce.Media.Count > 0 || ce.FotograferingIkkeTillatt),
            new("Alle påkrevde felt er fylt ut", AlleFeltFyltUt(ce))
        ];
    }

    public static bool ErGodkjent(CeGodkjenning ce, List<DorKomponent> dorKomponenter) =>
        Evaluer(ce, dorKomponenter).All(r => r.Bestatt);

    // Risikofelt - "Ja" er svaret som feiler.
    public static bool ErKuttskaderisikoOk(CeGodkjenning ce) => ce.KuttskadeRisiko != true;
    public static bool ErGlassFareOk(CeGodkjenning ce) => ce.GlassFareKuttSkade != true;

    // Krav-/funksjonsfelt - "Nei" er svaret som feiler.
    public static bool ErGlassSynligTiltakOk(CeGodkjenning ce) => ce.GlassIDor != "With glass" || ce.GlassSynligTiltak == true;
    public static bool ErGlassOk(CeGodkjenning ce) => ErGlassSynligTiltakOk(ce) && ErGlassFareOk(ce);
    public static bool ErFriBreddeOk(CeGodkjenning ce) => ce.FriBredde086 == true;
    public static bool ErTerskelOk(CeGodkjenning ce) => ce.TerskelUnder25mm == true;
    public static bool ErDodlasOk(CeGodkjenning ce) => ce.DodlasEtterStopp == true;
    public static bool ErForsinkelseOk(CeGodkjenning ce) => ce.ForsinkelseForLukking == true;
    public static bool ErSensorplasseringOk(CeGodkjenning ce) => ce.SensorplasseringKorrekt == true;
    public static bool ErReaksjonstidOk(CeGodkjenning ce) => ce.ReaksjonstidOk == true;
    public static bool ErBrannalarmUtkoblingOk(CeGodkjenning ce) => ce.SikkerhetssensorUtkoblingBrannalarm == true;
    public static bool ErImpulsbryterOk(CeGodkjenning ce) => ce.ImpulsbryterKorrektHoyde == true;
    public static bool ErAktiveringsbryterOk(CeGodkjenning ce) => ce.AktiveringsbryterFriPlass == true;
    public static bool ErSkiltingOk(CeGodkjenning ce) => ce.TydeligSkilting == true;
    public static bool ErHengselsideOk(CeGodkjenning ce) => ce.HengselsideBeskyttet == true;
    public static bool ErLaskoblingOk(CeGodkjenning ce) => ce.ElektroniskLasKoblingTestet == true;

    // Målte verdier mot standardens grenseverdier - samme sjekk som hard-blokkeringen i veiviseren.
    public static bool ErApningstidOk(CeGodkjenning ce, CeMaleGrenseverdier g) =>
        ce.ApningstidUnntatt || ce.ApningstidSek is null || ce.ApningstidSek <= g.MaksApningstidSek;

    public static bool ErLukketidHoyOk(CeGodkjenning ce, CeMaleGrenseverdier g) =>
        ce.LukketidHoyUnntatt || ce.LukketidHoySek is null || ce.LukketidHoySek <= g.MaksLukketidHoySek;

    public static bool ErLukketidLavOk(CeGodkjenning ce, CeMaleGrenseverdier g) =>
        ce.LukketidLavUnntatt || ce.LukketidLavSek is null || ce.LukketidLavSek <= g.MaksLukketidLavSek;

    public static bool ErApningskraftOk(CeGodkjenning ce, CeMaleGrenseverdier g) =>
        ce.ApningskraftUnntatt || ce.ApningskraftN is null || ce.ApningskraftN <= g.MaksApningskraftN;

    private static bool AlleFeltFyltUt(CeGodkjenning ce) =>
        !string.IsNullOrWhiteSpace(ce.GlassIDor)
        && ce.FriBredde086.HasValue
        && ce.TerskelUnder25mm.HasValue
        && ce.KuttskadeRisiko.HasValue
        && ce.Apningsvinkel.HasValue
        && ce.ApningstidSek.HasValue
        && ce.LukketidHoySek.HasValue
        && ce.LukketidLavSek.HasValue
        && ce.ApningskraftN.HasValue
        && ce.DodlasEtterStopp.HasValue
        && ce.ForsinkelseForLukking.HasValue
        && ce.ApnesMotGjennomgangstrafikk.HasValue
        && ce.SensorplasseringKorrekt.HasValue
        && ce.ReaksjonstidOk.HasValue
        && ce.SikkerhetssensorUtkoblingBrannalarm.HasValue
        && ce.NodapningTestet.HasValue
        && ce.ImpulsbryterKorrektHoyde.HasValue
        && ce.AktiveringsbryterFriPlass.HasValue
        && ce.TydeligSkilting.HasValue
        && ce.HengselsideBeskyttet.HasValue
        && ce.ElektroniskLasKoblingTestet.HasValue
        && !string.IsNullOrWhiteSpace(ce.UtfortAvNavn);
}
