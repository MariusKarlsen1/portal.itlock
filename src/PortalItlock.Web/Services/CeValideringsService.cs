using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public record CeValideringsResultat(string Tekst, bool Bestatt);

// Regelmotor for steg 9 (Validation) i CE-godkjenningen. Reglene er et tilpasset
// sett basert på felt portalen faktisk sporer (dørdesign, mål, kontrollsvar,
// funksjonstest og utfyllingsgrad) - ikke en juridisk fasit, men en praktisk
// fullstendighets-/rimelighetssjekk før godkjenningen sendes til gjennomgang.
public static class CeValideringsService
{
    public static List<CeValideringsResultat> Evaluer(CeGodkjenning ce, List<DorKomponent> dorKomponenter)
    {
        var sikkerhetsTilbehor = dorKomponenter
            .Where(k => k.Component?.Type?.CeKategori == CeTilbehorKategori.SikkerhetsTilbehor)
            .ToList();

        var resultater = new List<CeValideringsResultat>
        {
            new("Kuttskaderisiko i dørdesign er vurdert",
                ce.KuttskadeRisiko.HasValue),

            new("Dørkonstruksjon og karmkonstruksjon er dokumentert",
                !string.IsNullOrWhiteSpace(ce.Dorkonstruksjon) && !string.IsNullOrWhiteSpace(ce.Karmkonstruksjon)),

            new("Hengselside er beskyttet",
                ce.HengselsideBeskyttet == true),

            new("Sensorer er installert og programmert korrekt",
                sikkerhetsTilbehor.Count > 0 && ce.SensorplasseringKorrekt == true && ce.ReaksjonstidOk == true),

            new("Aktiveringsenheter er korrekt installert",
                ce.AktiveringsbryterFriPlass == true && ce.ImpulsbryterKorrektHoyde == true),

            new("Funksjonstest-media er levert",
                ce.Media.Count > 0 || ce.FotograferingIkkeTillatt),

            new("Alle påkrevde felt er fylt ut",
                AlleFeltFyltUt(ce))
        };

        return resultater;
    }

    public static bool ErGodkjent(CeGodkjenning ce, List<DorKomponent> dorKomponenter) =>
        Evaluer(ce, dorKomponenter).All(r => r.Bestatt);

    private static bool AlleFeltFyltUt(CeGodkjenning ce) =>
        ce.GlassIDor.HasValue
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
        && ce.EkstraFunksjonerTestet.HasValue
        && !string.IsNullOrWhiteSpace(ce.UtfortAvNavn);
}
