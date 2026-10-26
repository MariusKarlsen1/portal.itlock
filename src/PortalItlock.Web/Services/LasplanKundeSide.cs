using System.Net;
using System.Text.Json;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

/// <summary>
/// Kundens side for å se, endre og godkjenne låsplanen (/lasplan/g/{token}).
/// Ren HTML med innebygde data og litt JavaScript - kunden er ikke innlogget,
/// så siden kan ikke bruke Blazor sin innloggede tilkobling. Godkjenningen
/// postes til /lasplan/g/{token}/godkjenn.
/// </summary>
public static class LasplanKundeSide
{
    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    public static string Melding(string org, string tittel, string tekst) => Ramme(org, tittel,
        $"<section class=\"kort\"><h2>{E(tittel)}</h2><p class=\"mut\">{E(tekst)}</p></section>", "");

    public static string Side(string org, Prosjekt p, LasplanGodkjenningService.Matrise m, string token)
    {
        if (p.LasplanStatus == LasplanStatus.Godkjent)
        {
            return Melding(org, "Låsplanen er godkjent",
                $"Signert av {p.LasplanGodkjentAv}{(p.LasplanGodkjentFirma is null ? "" : $", {p.LasplanGodkjentFirma}")} {p.LasplanGodkjentDato:dd.MM.yyyy HH:mm}. {org} har fått beskjed. Takk!");
        }
        if (!LasplanGodkjenningService.KanGodkjennes(p))
        {
            return Melding(org, "Lenken er ikke lenger gyldig",
                $"Låsplanen kan ikke godkjennes via denne lenken lenger. Ta kontakt med {org} for en ny lenke.");
        }

        var data = JsonSerializer.Serialize(new
        {
            token,
            nokler = m.Nokler.Select(n => new { n.Id, n.Merking, n.Navn, n.Antall }),
            rader = m.Rader.Select(r => new { r.Nokkel, r.Nr, r.DorTil, r.Dornr, r.Etasje, r.Sylinder, r.Antall }),
            kryss = m.Kryss
        }).Replace("</", "<\\/");

        var innhold = $"""
<section class="kort">
  <div class="hode"><span class="logo">{E(Initialer(org))}</span><div><small class="mut">Fra {E(org)}</small><h2>Låsplan til godkjenning – {E(p.Navn)}</h2></div></div>
  <div class="hint">Se over hvilke nøkler som skal åpne hvilke dører. Trykk i en rute for å legge til eller fjerne et kryss – endringer vises i gult. Når alt stemmer: skriv navn, signer og godkjenn nederst.</div>
  <div class="info">
    <div><small>Systemnr.</small><b>{E(string.IsNullOrWhiteSpace(p.LasplanSystemnr) ? "–" : p.LasplanSystemnr)}</b></div>
    <div><small>Systemeier</small><b>{E(p.Kunde?.Navn ?? "–")}</b></div>
    <div><small>Sylindere</small><b>{m.Rader.Count}</b></div>
    <div><small>Nøkler</small><b>{m.Nokler.Count} typer</b></div>
  </div>
  <div class="mx-r"><table class="mx" id="mx"></table></div>
  <div class="forklaring"><span><i class="x pa">X</i>Åpner</span><span><i class="x ny">X</i>Lagt til av deg</span><span><i class="x fjernet"></i>Fjernet av deg</span></div>
  <form class="godkjenn" id="skjema" novalidate>
    <h3>Godkjenn låsplanen</h3>
    <div class="to">
      <label>Fullt navn<input id="navn" autocomplete="name" required maxlength="120"></label>
      <label>Firma / rolle<input id="firma" autocomplete="organization" maxlength="120"></label>
    </div>
    <div class="signer"><canvas id="sign" aria-label="Signaturfelt"></canvas><span>Signer her med mus eller finger</span></div>
    <label class="cb"><input type="checkbox" id="ok"> Jeg har kontrollert låsplanen og godkjenner den<span id="endr" class="mut"></span></label>
    <p class="feil" id="feil" role="alert" hidden></p>
    <div class="knapper"><button type="button" class="knapp" id="tom">Tøm signatur</button><button type="submit" class="knapp gronn" id="send">Godkjenn og signer</button></div>
  </form>
</section>
<script type="application/json" id="data">{data}</script>
""";
        return Ramme(org, $"Låsplan – {p.Navn}", innhold, Skript);
    }

    private static string Initialer(string navn) => string.Concat(navn.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(s => char.ToUpperInvariant(s[0])));

    private static string Ramme(string org, string tittel, string innhold, string skript) => $$"""
<!doctype html>
<html lang="no">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex">
<title>{{E(tittel)}}</title>
<style>
:root { --side:#f7f3ea; --kort:#fff; --ink:#2a2622; --muted:#6b6863; --line:#e3dccf; --line2:#f0ebe4; --brun:#835e41; --brun-lys:#f3e9dc; --gronn:#2e7d4f; --gul:#a8691f; --gul-lys:#fbf0dc; --bla:#2f5d99; --bla-lys:#e6eef8; --rod:#b23b22; }
* { box-sizing: border-box; }
body { margin: 0; background: var(--side); color: var(--ink); font-family: "Segoe UI", system-ui, sans-serif; font-size: 15px; }
main { max-width: 1200px; margin: 0 auto; padding: 22px 16px 60px; }
.kort { background: var(--kort); border: 1px solid var(--line); border-radius: 20px; padding: 18px; display: grid; gap: 14px; box-shadow: 0 14px 34px rgba(60,35,20,.07); }
h2 { margin: 0; font-size: 1.2rem; } h3 { margin: 0; font-size: 1rem; }
.mut { color: var(--muted); }
.hode { display: flex; gap: 12px; align-items: center; }
.logo { width: 42px; height: 42px; border-radius: 12px; background: var(--brun); color: #fff; display: grid; place-items: center; font-weight: 800; flex: none; }
.hint { background: var(--bla-lys); color: var(--bla); border-radius: 12px; padding: 10px 12px; font-size: .9rem; }
.info { display: grid; grid-template-columns: repeat(4, minmax(0,1fr)); gap: 10px; }
@media (max-width: 700px) { .info { grid-template-columns: 1fr 1fr; } }
.info div { border: 1px solid var(--line); border-radius: 12px; padding: 9px 12px; }
.info small { display: block; font-size: .68rem; font-weight: 700; letter-spacing: .06em; text-transform: uppercase; color: var(--muted); }
.mx-r { border: 1px solid var(--line); border-radius: 14px; overflow: auto; max-height: 70vh; }
table.mx { border-collapse: separate; border-spacing: 0; font-size: .86rem; min-width: 100%; }
.mx th, .mx td { border-bottom: 1px solid var(--line2); padding: 7px 10px; background: var(--kort); white-space: nowrap; }
.mx thead th { position: sticky; top: 0; z-index: 3; background: #fbf8f3; font-size: .68rem; font-weight: 700; letter-spacing: .05em; text-transform: uppercase; color: var(--muted); text-align: left; vertical-align: bottom; border-bottom: 1px solid var(--line); }
.mx .fast { position: sticky; left: 0; z-index: 2; }
.mx thead .fast { z-index: 4; }
.mx th.nk { width: 42px; min-width: 42px; padding: 8px 4px; text-align: center; border-left: 1px solid var(--line2); }
.nk-inn { display: flex; flex-direction: column; align-items: center; gap: 6px; }
.nk-inn span { writing-mode: vertical-rl; transform: rotate(180deg); text-transform: none; letter-spacing: 0; font-size: .8rem; font-weight: 600; color: var(--ink); max-height: 150px; overflow: hidden; text-overflow: ellipsis; }
.nk-inn b { font-size: .74rem; color: var(--brun); }
.nk-inn em { font-style: normal; font-size: .66rem; color: var(--muted); }
.mx td.c { text-align: center; padding: 0; border-left: 1px solid var(--line2); }
.mx td.c button { width: 42px; height: 34px; border: 0; background: none; display: grid; place-items: center; cursor: pointer; }
.mx td.c button:hover { background: var(--brun-lys); }
.mx td.c button:focus-visible { outline: 2px solid var(--brun); outline-offset: -2px; }
.x { width: 22px; height: 22px; border-radius: 6px; display: grid; place-items: center; font-weight: 800; font-size: 1rem; font-style: normal; line-height: 1; }
.x.pa { color: var(--ink); }
.x.ny { color: var(--gul); background: var(--gul-lys); }
.x.fjernet { border: 1.5px dashed var(--gul); }
.nr { color: var(--brun); font-weight: 700; }
.syl { font-weight: 600; }
.forklaring { display: flex; gap: 14px; flex-wrap: wrap; font-size: .8rem; color: var(--muted); }
.forklaring span { display: inline-flex; gap: 6px; align-items: center; }
.forklaring .x { width: 16px; height: 16px; font-size: .8rem; }
.godkjenn { border: 1px solid var(--line); border-radius: 14px; padding: 14px; display: grid; gap: 10px; }
.to { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
@media (max-width: 600px) { .to { grid-template-columns: 1fr; } }
label { display: grid; gap: 5px; font-size: .8rem; font-weight: 700; color: var(--muted); }
input { font: inherit; font-weight: 400; color: var(--ink); border: 1px solid var(--line); border-radius: 10px; padding: 9px 11px; background: #fff; }
input:focus { outline: none; border-color: var(--brun); box-shadow: 0 0 0 3px rgba(131,94,65,.14); }
.signer { border: 1.5px dashed #cfc6b9; border-radius: 12px; height: 130px; position: relative; background: #fff; touch-action: none; }
.signer canvas { width: 100%; height: 100%; display: block; border-radius: 12px; }
.signer span { position: absolute; left: 12px; bottom: 8px; font-size: .74rem; color: #9a9389; pointer-events: none; }
.cb { display: flex; gap: 8px; align-items: flex-start; font-size: .9rem; font-weight: 400; color: var(--ink); }
.cb input { width: 18px; height: 18px; margin: 1px 0 0; }
.knapper { display: flex; gap: 8px; justify-content: flex-end; flex-wrap: wrap; }
.knapp { font: inherit; display: inline-flex; align-items: center; gap: 7px; border: 1px solid var(--line); background: #fff; border-radius: 10px; padding: 10px 16px; font-size: .9rem; font-weight: 600; cursor: pointer; color: var(--ink); }
.knapp.gronn { background: var(--gronn); border-color: var(--gronn); color: #fff; }
.knapp:disabled { opacity: .6; cursor: default; }
.feil { margin: 0; color: var(--rod); font-weight: 600; font-size: .9rem; }
.feil[hidden] { display: none; }
</style>
</head>
<body><main>{{innhold}}</main>{{skript}}</body>
</html>
""";

    private const string Skript = """
<script>
(function () {
  var d = JSON.parse(document.getElementById("data").textContent);
  var start = new Set(d.kryss), na = new Set(d.kryss);
  var $ = function (id) { return document.getElementById(id); };
  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
  function k(n, r) { return n + "|" + r; }
  function tegn() {
    var h = '<thead><tr><th class="fast">Syl.</th><th>Dør til</th><th>Dørnr.</th><th>Etg.</th><th>Sylinder</th><th>Ant.</th>' +
      d.nokler.map(function (n) { return '<th class="nk"><div class="nk-inn"><span>' + esc(n.Navn) + '</span><b>' + esc(n.Merking) + '</b><em>' + n.Antall + ' stk</em></div></th>'; }).join("") + '</tr></thead><tbody>';
    h += d.rader.map(function (r) {
      return '<tr><td class="fast"><span class="nr">' + esc(r.Nr) + '</span></td><td class="syl">' + esc(r.DorTil || "–") + '</td><td>' + esc(r.Dornr || "Reserve") + '</td><td>' + esc(r.Etasje || "") + '</td><td>' + esc(r.Sylinder) + '</td><td>' + r.Antall + '</td>' +
        d.nokler.map(function (n) {
          var key = k(n.Id, r.Nokkel), pa = na.has(key), var_ = start.has(key);
          var cls = pa !== var_ ? (pa ? "ny" : "fjernet") : (pa ? "pa" : "");
          return '<td class="c"><button type="button" data-k="' + esc(key) + '" aria-pressed="' + pa + '" aria-label="' + esc(n.Navn) + ' åpner ' + esc(r.DorTil || r.Sylinder) + '"><i class="x ' + cls + '">' + (pa ? "X" : "") + '</i></button></td>';
        }).join("") + '</tr>';
    }).join("") + '</tbody>';
    $("mx").innerHTML = h;
    var e = 0; start.forEach(function (x) { if (!na.has(x)) e++; }); na.forEach(function (x) { if (!start.has(x)) e++; });
    $("endr").textContent = e ? " – inkludert " + e + " endring" + (e === 1 ? "" : "er") : "";
  }
  $("mx").addEventListener("click", function (ev) {
    var b = ev.target.closest("button[data-k]"); if (!b) return;
    var key = b.getAttribute("data-k");
    if (na.has(key)) na.delete(key); else na.add(key);
    tegn();
    var ny = $("mx").querySelector('button[data-k="' + key.replace(/"/g, "") + '"]'); if (ny) ny.focus();
  });
  tegn();

  var c = $("sign"), ctx, tegnet = false, ned = false;
  function klargjor() { var r = c.getBoundingClientRect(); c.width = r.width * 2; c.height = r.height * 2; ctx = c.getContext("2d"); ctx.scale(2, 2); ctx.lineWidth = 2.2; ctx.lineCap = "round"; ctx.lineJoin = "round"; ctx.strokeStyle = "#2a2622"; tegnet = false; }
  klargjor();
  function pos(e) { var b = c.getBoundingClientRect(); return [e.clientX - b.left, e.clientY - b.top]; }
  c.addEventListener("pointerdown", function (e) { ned = true; c.setPointerCapture(e.pointerId); var p = pos(e); ctx.beginPath(); ctx.moveTo(p[0], p[1]); });
  c.addEventListener("pointermove", function (e) { if (!ned) return; var p = pos(e); ctx.lineTo(p[0], p[1]); ctx.stroke(); tegnet = true; });
  c.addEventListener("pointerup", function () { ned = false; });
  c.addEventListener("pointercancel", function () { ned = false; });
  $("tom").addEventListener("click", klargjor);

  function feil(t) { var f = $("feil"); f.textContent = t; f.hidden = !t; }
  $("skjema").addEventListener("submit", function (ev) {
    ev.preventDefault(); feil("");
    var navn = $("navn").value.trim();
    if (navn.length < 2) { feil("Skriv inn fullt navn."); $("navn").focus(); return; }
    if (!tegnet) { feil("Signer i feltet over."); return; }
    if (!$("ok").checked) { feil("Kryss av for at du har kontrollert låsplanen."); return; }
    var knapp = $("send"); knapp.disabled = true; knapp.textContent = "Sender …";
    fetch(location.pathname.replace(/\/$/, "") + "/godkjenn", {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ navn: navn, firma: $("firma").value.trim(), signatur: c.toDataURL("image/png"), kryss: Array.from(na) })
    }).then(function (r) { return r.json().catch(function () { return { ok: false, melding: "Noe gikk galt. Prøv igjen." }; }); })
      .then(function (s) {
        if (s.ok) { location.reload(); return; }
        feil(s.melding || "Noe gikk galt. Prøv igjen."); knapp.disabled = false; knapp.textContent = "Godkjenn og signer";
      })
      .catch(function () { feil("Fikk ikke kontakt. Sjekk nettet og prøv igjen."); knapp.disabled = false; knapp.textContent = "Godkjenn og signer"; });
  });
})();
</script>
""";
}
