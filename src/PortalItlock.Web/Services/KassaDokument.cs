using System.Globalization;
using System.Net;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Kontantfaktura, kreditnota og dagsrapport fra kassa som HTML: til utskrift
// på kvitteringsskriver (80 mm) eller vanlig skriver, og som e-post.
// Innholdet følger kravene til salgsdokument i bokføringsforskriften kap. 5-1:
// nummer, dato, selger med org.nr., hva som er solgt, beløp og mva per sats.
public static class KassaDokument
{
    public sealed record Firma(string Navn, string? OrgNr, string? Adresse, string? Telefon, bool MvaRegistrert, string? Bunntekst);

    public static Firma FraTenant(Tenant? t, KasseOppsett oppsett) => new(
        t?.PortalVisningsnavn ?? t?.Navn ?? "Full Kontroll",
        t?.OrgNr,
        string.Join(", ", new[] { t?.Adresse, string.Join(" ", new[] { t?.Postnr, t?.Sted }.Where(s => !string.IsNullOrWhiteSpace(s))) }.Where(s => !string.IsNullOrWhiteSpace(s))),
        t?.Telefon,
        oppsett.MvaRegistrert,
        oppsett.Bunntekst);

    private static readonly CultureInfo Nb = CultureInfo.GetCultureInfo("nb-NO");
    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
    public static string Kr(decimal v) => v.ToString("N2", Nb);

    private static string Rad(string v, string h, string klasse = "") => $"<div class=\"rad {klasse}\"><span>{v}</span><span>{h}</span></div>";

    // Selve kassalappen (uten <html>), brukt både i utskrift og e-post.
    public static string Lapp(KasseSalg s, Firma f, KasseSalg? gjelder = null)
    {
        var kred = s.Type == KasseSalgType.Kreditnota;
        var linjer = string.Concat(s.Linjer.Select(l =>
            $"<div>{E(l.Navn)}</div>" + Rad($"&nbsp;&nbsp;{l.Antall} x {Kr(l.Enhetspris)}{(l.RabattProsent > 0 ? $" -{l.RabattProsent:0.#}%" : "")}", Kr(l.Belop))));
        var mva = string.Concat(KassaService.MvaFordeling(s.Linjer).Select(m =>
            Rad($"Mva {m.Sats:0.#} % av {Kr(m.Grunnlag)}", Kr(m.Mva))));
        var betaling = kred
            ? Rad(s.BetaltTilbakeMed == KasseSalgType.Kort ? "Refusjon kort" : "Tilbake kontant", Kr(-s.Total))
            : s.Type == KasseSalgType.Kort
                ? Rad("Betalt kort", Kr(s.Total))
                : Rad("Kontant", Kr(s.Mottatt ?? s.Total)) + (s.Mottatt is { } m2 ? Rad("Veksel", Kr(m2 - s.Total)) : "");
        return $"""
<div class="lapp">
<div class="midt"><b class="firma">{E(f.Navn)}</b><br>{E(f.Adresse)}{(string.IsNullOrWhiteSpace(f.Telefon) ? "" : $"<br>Tlf {E(f.Telefon)}")}{(string.IsNullOrWhiteSpace(f.OrgNr) ? "" : $"<br>Org.nr {E(f.OrgNr)}{(f.MvaRegistrert ? " MVA" : "")}")}</div>
<hr><div class="midt tittel">{(kred ? "KREDITNOTA" : "KONTANTFAKTURA")}</div>
{Rad("Nr", s.Nummer.ToString(CultureInfo.InvariantCulture))}{Rad("Dato", s.Tidspunkt.ToString("dd.MM.yyyy HH:mm", Nb))}
{(kred ? Rad("Krediterer", gjelder is null ? "" : $"Kontantfaktura {gjelder.Nummer}") + Rad("Årsak", E(s.Arsak)) : "")}
{(string.IsNullOrWhiteSpace(s.KundeNavn) ? "" : Rad("Kunde", E(s.KundeNavn)))}
<hr>{linjer}<hr>
{Rad(kred ? "TILBAKE" : "TOTALT", Kr(Math.Abs(s.Total)), "tot")}
{(f.MvaRegistrert ? mva : "<div>Ikke mva-registrert</div>")}
<hr>{betaling}<hr>
<div class="midt">Levert {s.Tidspunkt:dd.MM.yyyy} · {E(s.KasseNavn)}{(string.IsNullOrWhiteSpace(s.BrukerNavn) ? "" : $" · {E(s.BrukerNavn)}")}{(string.IsNullOrWhiteSpace(f.Bunntekst) ? "" : $"<br>{E(f.Bunntekst)}")}</div>
</div>
""";
    }

    public const string LappStil = """
.lapp{background:#fff;width:300px;max-width:100%;margin:0 auto;padding:16px 14px 20px;font-family:"Cascadia Mono",Consolas,ui-monospace,monospace;font-size:12px;line-height:1.55;color:#111}
.lapp .midt{text-align:center}.lapp .firma{font-size:14px;letter-spacing:.04em}.lapp .tittel{font-weight:700;letter-spacing:.08em}
.lapp .rad{display:flex;justify-content:space-between;gap:10px}.lapp .tot{font-size:14px;font-weight:700}
.lapp hr{border:0;border-top:1px dashed #888;margin:8px 0}
""";

    // Egen side til utskrift. 80 mm-skrivere bruker bredden på lappen.
    public static string UtskriftSide(string tittel, string innhold) => $$$"""
<!doctype html><html lang="no"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><meta name="robots" content="noindex"><title>{{{E(tittel)}}}</title>
<style>body{margin:0;background:#f4f0ea;font-family:"Segoe UI",system-ui,sans-serif}{{{LappStil}}}
.verktoy{display:flex;gap:8px;justify-content:center;padding:14px}.verktoy button{font:inherit;font-weight:700;border:0;border-radius:10px;background:#231c16;color:#fff;padding:10px 18px;cursor:pointer}
.lapp{box-shadow:0 8px 24px rgba(0,0,0,.1);margin-bottom:24px}
@media print{body{background:#fff}.verktoy{display:none}.lapp{box-shadow:none;margin:0;width:auto;padding:0}@page{margin:4mm}}</style></head>
<body><div class="verktoy"><button onclick="window.print()">Skriv ut</button></div>{{{innhold}}}<script>if(location.search.indexOf('skriv=1')>=0)window.addEventListener('load',()=>window.print());</script></body></html>
""";

    public static string Epost(KasseSalg s, Firma f, KasseSalg? gjelder = null) => $"""
<div style="font-family:Segoe UI,Arial,sans-serif;color:#222">
<p>Hei{(string.IsNullOrWhiteSpace(s.KundeNavn) ? "" : " " + E(s.KundeNavn))},</p>
<p>Her er {(s.Type == KasseSalgType.Kreditnota ? "kreditnotaen" : "kontantfakturaen")} for kjøpet ditt hos {E(f.Navn)}.</p>
<style>{LappStil}</style>
<div style="background:#f4f0ea;padding:16px">{Lapp(s, f, gjelder)}</div>
</div>
""";

    public static string Dagsrapport(KasseDagsoppgjor o, Firma f, IReadOnlyList<KassaService.Mvalinje> mva) => $"""
<div class="lapp">
<div class="midt"><b class="firma">{E(f.Navn)}</b><br>{E(o.KasseNavn)} · {o.Lukket:dd.MM.yyyy}</div><hr>
<div class="midt tittel">DAGSRAPPORT (Z)</div>
{Rad("Antall salg", o.AntallSalg.ToString(CultureInfo.InvariantCulture))}{Rad("Kreditnotaer", o.AntallKreditnota.ToString(CultureInfo.InvariantCulture))}{Rad("Bilag", o.FraNummer is null ? "–" : $"{o.FraNummer}–{o.TilNummer}")}<hr>
{Rad("Kort", Kr(o.KortIKassa))}{Rad("Kontant", Kr(o.KontantIKassa))}{Rad("OMSETNING", Kr(o.Omsetning), "tot")}
{string.Concat(mva.Select(m => Rad($"Mva {m.Sats:0.#} % av {Kr(m.Grunnlag)}", Kr(m.Mva))))}<hr>
{Rad("Terminal", Kr(o.TerminalTotal))}{Rad("Differanse kort", Kr(o.DifferanseKort))}<hr>
{Rad("Veksel start", Kr(o.VekselStart))}{Rad("Opptalt", Kr(o.Opptalt))}{Rad("Kassedifferanse", Kr(o.DifferanseKontant))}{Rad("Til bank", Kr(o.TilBank))}
{(string.IsNullOrWhiteSpace(o.Forklaring) ? "" : $"<hr><div>Forklaring: {E(o.Forklaring)}</div>")}
<hr><div class="midt">Lukket {o.Lukket:dd.MM.yyyy HH:mm}{(string.IsNullOrWhiteSpace(o.LukketAv) ? "" : $" av {E(o.LukketAv)}")}</div>
</div>
""";
}
