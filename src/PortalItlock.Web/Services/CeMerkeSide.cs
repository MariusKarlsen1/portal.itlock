using System.Net;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// CE-merket er et eget, rundt merke med QR som settes på automatikkdøra når
// CE er godkjent. Det er ikke dør-QR-en: merket byttes ved hver årlige
// kontroll, får ny farge hvert år, og QR-en peker på en statusside
// (/ce/m/{kode}) som alle kan åpne uten å logge inn. Skannes fjorårets merke,
// står det at merket er utløpt.
public static class CeMerkeSide
{
    public enum Tilstand { Gyldig, Forfalt, Utlopt, IkkeFerdig }

    // Én farge per år, i fast rekkefølge, så det er lett å se på døra om
    // merket er fra i år.
    private static readonly string[] Farger = ["#7a55b0", "#c26a1f", "#2f7a3a", "#2f5f8a"];

    public static string Farge(int aar) => Farger[((aar % Farger.Length) + Farger.Length) % Farger.Length];

    public static int Aar(CeGodkjenning ce) => (ce.UtfortAvDato ?? ce.GyldigFra).Year;

    public static string Url(string baseUrl, string token) => $"{baseUrl}/ce/m/{token}";

    // historikk = alle CE-godkjenningene på døra.
    public static Tilstand Finn(CeGodkjenning ce, IEnumerable<CeGodkjenning> historikk)
    {
        var nyereGodkjent = historikk.Any(h => h.Id != ce.Id && h.Status == CeGodkjenningStatus.Godkjent && h.Id > ce.Id);
        if (nyereGodkjent)
        {
            return Tilstand.Utlopt;
        }

        if (ce.Status != CeGodkjenningStatus.Godkjent)
        {
            return Tilstand.IkkeFerdig;
        }

        return ce.GyldigTil < DateTime.Now ? Tilstand.Forfalt : Tilstand.Gyldig;
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    public static string Melding(string org, string tittel, string tekst) => Ramme(tittel,
        $"<section class=\"kort\"><h2>{E(tittel)}</h2><p class=\"mut\">{E(tekst)}</p><p class=\"mut\">{E(org)}</p></section>");

    public static string Status(string org, CeGodkjenning ce, IReadOnlyList<CeGodkjenning> historikk)
    {
        var tilstand = Finn(ce, historikk);
        var aar = Aar(ce);
        var (farge, ikon, tittel, tekst) = tilstand switch
        {
            Tilstand.Gyldig => ("#2f7a3a", "✓", "CE-godkjent", $"Gyldig til {ce.GyldigTil:dd.MM.yyyy}"),
            Tilstand.Forfalt => ("#b4372b", "!", "Kontrollen er forfalt", $"Godkjenningen gikk ut {ce.GyldigTil:dd.MM.yyyy}. Døra skal kontrolleres på nytt."),
            Tilstand.Utlopt => ("#b4372b", "!", "Merket er utløpt", $"Dette er merket fra {aar}. Døra har fått nytt CE-merke – skann det nye."),
            _ => ("#b7791f", "…", "CE er ikke fullført", "CE-godkjenningen av døra pågår.")
        };

        var dor = string.Join(" · ", new[] { ce.Dornummer, ce.DorTil }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var automatikk = string.Join(" ", new[] { ce.Produsent, ce.ItemNavn }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var sted = string.Join(", ", new[] { ce.ProsjektNavn, ce.Adresse }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var detaljer = new List<(string, string)>();
        if (ce.UtfortAvDato is { } utfort)
        {
            detaljer.Add(("Sist kontrollert", $"{utfort:dd.MM.yyyy}{(string.IsNullOrWhiteSpace(ce.UtfortAvNavn) ? "" : " av " + ce.UtfortAvNavn)}"));
        }

        if (tilstand is Tilstand.Gyldig or Tilstand.Forfalt)
        {
            detaljer.Add(("Neste kontroll", ce.GyldigTil.ToString("dd.MM.yyyy")));
        }

        if (sted.Length > 0) detaljer.Add(("Sted", sted));
        if (!string.IsNullOrWhiteSpace(ce.Etasje)) detaljer.Add(("Etasje", ce.Etasje!));
        detaljer.Add(("Sertifikatnummer", ce.Sertifiseringsnummer));
        detaljer.Add(("Ansvarlig firma", org));

        var godkjente = historikk.Where(h => h.Status == CeGodkjenningStatus.Godkjent).OrderByDescending(h => h.Id).ToList();
        var hist = string.Concat(godkjente.Select((h, i) =>
            $"<div class=\"hist\"><span class=\"prikk\" style=\"background:{Farge(Aar(h))}\"></span><span class=\"t\"><b>{(i == godkjente.Count - 1 ? "Første CE-godkjenning" : "Årlig kontroll – godkjent")}</b><small>{(h.UtfortAvDato ?? h.GyldigFra):dd.MM.yyyy}{(string.IsNullOrWhiteSpace(h.UtfortAvNavn) ? "" : " · " + E(h.UtfortAvNavn))}</small></span>{(h.Id == ce.Id ? "<em>Dette merket</em>" : "")}</div>"));

        var innhold = $"""
<p class="org">{E(org)}</p>
<section class="status" style="background:{farge}"><div class="ring">{ikon}</div><h1>{E(tittel)}</h1><p>{E(tekst)}</p></section>
<section class="kort merke-rad">
  <div class="sticker" style="background:{Farge(aar)}"><span class="aar">{aar}</span><span class="ce">CE</span></div>
  <div><b>{E(dor.Length > 0 ? dor : "Automatisk dør")}</b>{(automatikk.Length > 0 ? $"<br>{E(automatikk)}" : "")}<br><span class="mut">Merke {aar}</span></div>
</section>
<section class="kort"><h2>Detaljer</h2>{string.Concat(detaljer.Select(d => $"<div class=\"rad\"><small>{E(d.Item1)}</small><b>{E(d.Item2)}</b></div>"))}</section>
{(godkjente.Count > 0 ? $"<section class=\"kort\"><h2>Historikk</h2>{hist}</section>" : "")}
<p class="fot">Automatiske dører skal kontrolleres minst én gang i året (NS-EN 16005). Ved feil på døra: ta kontakt med {E(org)}.</p>
""";
        return Ramme($"CE-status – {dor}", innhold);
    }

    // Utskrift av merket: rundt klistremerke, ca. 50 mm, med QR i midten.
    public static string Utskrift(CeGodkjenning ce, string qrAdresse)
    {
        var aar = Aar(ce);
        var innhold = $"""
<div class="utskrift">
  <div class="sticker stor" style="background:{Farge(aar)}"><span class="aar">{aar}</span><img src="{E(qrAdresse)}" alt="QR"><span class="ce">CE</span></div>
  <p class="nr">{E(ce.Sertifiseringsnummer)}<br>{E(string.Join(" · ", new[] { ce.Dornummer, ce.DorTil }.Where(s => !string.IsNullOrWhiteSpace(s))))}</p>
  <button type="button" onclick="window.print()">Skriv ut</button>
</div>
""";
        return Ramme($"CE-merke {ce.Sertifiseringsnummer}", innhold);
    }

    private static string Ramme(string tittel, string innhold) => $$"""
<!doctype html>
<html lang="no">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex">
<title>{{E(tittel)}}</title>
<style>
:root { --side:#f7f3ea; --kort:#fff; --ink:#2a2622; --muted:#6b6863; --line:#e3dccf; }
* { box-sizing: border-box; }
body { margin: 0; background: var(--side); color: var(--ink); font-family: "Segoe UI", system-ui, sans-serif; font-size: 15px; }
main { max-width: 560px; margin: 0 auto; padding: 18px 14px 40px; display: grid; gap: 12px; }
.org { margin: 0; font-weight: 700; color: #835e41; text-align: center; }
.kort { background: var(--kort); border: 1px solid var(--line); border-radius: 18px; padding: 14px 16px; box-shadow: 0 8px 24px rgba(60,35,20,.06); }
h2 { margin: 0 0 6px; font-size: .78rem; letter-spacing: .07em; text-transform: uppercase; color: var(--muted); }
.mut { color: var(--muted); }
.status { border-radius: 20px; padding: 22px 16px; text-align: center; color: #fff; }
.status h1 { margin: 10px 0 4px; font-size: 1.5rem; }
.status p { margin: 0; opacity: .92; }
.ring { width: 64px; height: 64px; border-radius: 50%; background: rgba(255,255,255,.22); margin: 0 auto; display: grid; place-items: center; font-size: 30px; font-weight: 800; }
.merke-rad { display: flex; gap: 14px; align-items: center; line-height: 1.5; }
.sticker { width: 78px; height: 78px; border-radius: 50%; flex: none; position: relative; color: #fff; font-weight: 900; box-shadow: 0 4px 14px rgba(0,0,0,.15); }
.sticker .aar { position: absolute; top: 14px; left: 0; right: 0; text-align: center; font-size: 14px; letter-spacing: .06em; }
.sticker .ce { position: absolute; bottom: 14px; left: 0; right: 0; text-align: center; font-size: 22px; letter-spacing: .1em; }
.sticker.stor { width: 50mm; height: 50mm; display: flex; align-items: center; justify-content: center; box-shadow: none; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
.sticker.stor img { width: 25mm; height: 25mm; background: #fff; border-radius: 2mm; padding: 1mm; }
.sticker.stor .aar { top: 4mm; font-size: 4.2mm; }
.sticker.stor .ce { bottom: 3.5mm; font-size: 4.6mm; }
.rad { padding: 8px 0; border-bottom: 1px solid #f0ebe4; }
.rad:last-child { border-bottom: 0; }
.rad small { display: block; color: var(--muted); font-size: .78rem; }
.hist { display: flex; gap: 10px; align-items: center; padding: 9px 0; border-bottom: 1px solid #f0ebe4; }
.hist:last-child { border-bottom: 0; }
.hist .prikk { width: 12px; height: 12px; border-radius: 50%; flex: none; }
.hist .t { flex: 1; }
.hist small { display: block; color: var(--muted); }
.hist em { font-style: normal; font-size: .75rem; font-weight: 700; color: #2f7a3a; background: #e6f3e6; border-radius: 99px; padding: 2px 8px; }
.fot { font-size: .8rem; color: var(--muted); text-align: center; margin: 4px 8px 0; }
.utskrift { display: grid; justify-items: center; gap: 10px; padding-top: 30px; }
.utskrift .nr { margin: 0; text-align: center; font-size: 12px; color: var(--muted); }
.utskrift button { font: inherit; font-weight: 700; border: 0; border-radius: 12px; background: #835e41; color: #fff; padding: 12px 22px; cursor: pointer; }
@media print { body { background: #fff; } .utskrift { padding: 0; } .utskrift button { display: none; } }
</style>
</head>
<body><main>{{innhold}}</main></body>
</html>
""";
}
