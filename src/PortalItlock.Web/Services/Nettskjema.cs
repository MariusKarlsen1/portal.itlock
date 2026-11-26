using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Nettskjema for kunder: full-kontroll.no/skjema/{EpostNavn}. Kan deles som lenke
// eller legges inn på organisasjonens nettside i en iframe. Det som sendes inn blir
// en forespørsel med kanal «Skjema». Kunden får ingen e-post.
public static class Nettskjema
{
    public static readonly string[] StandardValg = ["Låsproblem", "Pris / tilbud", "Nøkler", "Adgangskontroll", "Annet"];

    public const int MaksBilder = 5;
    public const long MaksBildeStorrelse = 8 * 1024 * 1024;

    public static string[] Valg(TenantInnstillinger i)
    {
        var egne = (i.SkjemaValg ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => v.Length > 0)
            .Take(12)
            .ToArray();
        return egne.Length > 0 ? egne : StandardValg;
    }

    public static async Task<(Tenant Tenant, TenantInnstillinger Innstillinger)?> FinnAsync(PlatformDbContext platformDb, string navn)
    {
        var n = (navn ?? "").Trim().ToLowerInvariant();
        if (n.Length == 0)
        {
            return null;
        }

        var treff = await platformDb.TenantInnstillinger.AsNoTracking()
            .Where(i => i.EpostNavn != null && i.EpostNavn.ToLower() == n)
            .Join(platformDb.Tenants.Where(t => t.Status == TenantStatus.Aktiv), i => i.TenantId, t => t.Id, (i, t) => new { i, t })
            .FirstOrDefaultAsync();
        return treff is null ? null : (treff.t, treff.i);
    }

    // Enkel sperre mot spam: maks 5 innsendinger per IP-adresse per 10 minutter.
    private static readonly ConcurrentDictionary<string, List<DateTime>> Innsendinger = new();

    public static bool ForMange(string? ip)
    {
        var nokkel = ip ?? "ukjent";
        var na = DateTime.UtcNow;
        var liste = Innsendinger.GetOrAdd(nokkel, _ => []);
        lock (liste)
        {
            liste.RemoveAll(t => t < na.AddMinutes(-10));
            if (liste.Count >= 5)
            {
                return true;
            }
            liste.Add(na);
        }
        if (Innsendinger.Count > 5000)
        {
            Innsendinger.Clear();
        }
        return false;
    }

    public static string Firmanavn(Tenant t, TenantInnstillinger i) =>
        !string.IsNullOrWhiteSpace(i.AvsenderNavn) ? i.AvsenderNavn!
        : !string.IsNullOrWhiteSpace(t.PortalVisningsnavn) ? t.PortalVisningsnavn!
        : t.Navn;

    private static string H(string? s) => WebUtility.HtmlEncode(s ?? "");

    public sealed record Utfylt(string Navn = "", string Telefon = "", string Epost = "", string Adresse = "", string Kategori = "", string Tekst = "");

    public static string Side(Tenant t, TenantInnstillinger i, string? melding = null, string? feil = null, Utfylt? utfylt = null)
    {
        var navn = Firmanavn(t, i);
        var u = utfylt ?? new Utfylt();
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"no\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<title>").Append(H(navn)).Append(" – send forespørsel</title><style>");
        sb.Append("""
            *{box-sizing:border-box}body{margin:0;font-family:'Inter Tight',system-ui,-apple-system,'Segoe UI',Arial,sans-serif;color:#1d2433;background:#f4f5f7;font-size:15px}
            .ramme{max-width:520px;margin:0 auto;padding:24px 16px 40px}
            .kort{background:#fff;border:1px solid #dfdfde;border-radius:18px;padding:22px;box-shadow:0 1px 2px rgba(0,0,0,.04),0 10px 28px rgba(0,0,0,.06)}
            h1{margin:0 0 4px;font-size:1.25rem;color:#194783}p.under{margin:0 0 18px;color:#667085}
            label{display:block;font-weight:600;font-size:.85rem;margin:0 0 12px}
            input,textarea,select{display:block;width:100%;margin-top:5px;font:inherit;font-weight:400;border:1px solid #c9ccd2;border-radius:10px;padding:9px 11px;background:#fff;color:inherit}
            input:focus,textarea:focus{outline:2px solid #194783;outline-offset:0;border-color:#194783}
            textarea{min-height:110px;resize:vertical}.to{display:grid;grid-template-columns:1fr 1fr;gap:10px}
            .valg{display:flex;flex-wrap:wrap;gap:6px;margin:6px 0 14px}.valg label{margin:0;font-weight:500}
            .valg input{position:absolute;opacity:0;width:1px;height:1px}
            .valg span{display:inline-block;border:1px solid #c9ccd2;border-radius:999px;padding:6px 13px;cursor:pointer;font-size:.88rem}
            .valg input:checked+span{background:#194783;border-color:#194783;color:#fff}
            .valg input:focus-visible+span{outline:2px solid #194783;outline-offset:2px}
            .hp{position:absolute;left:-9999px;width:1px;height:1px;overflow:hidden}
            button{width:100%;height:44px;border:0;border-radius:11px;background:#194783;color:#fff;font:inherit;font-weight:600;cursor:pointer;margin-top:6px}
            .ok{background:#eaf5ee;color:#245f3c;border-radius:12px;padding:14px;margin-bottom:14px}
            .feil{background:#fdecea;color:#8a2a1a;border-radius:12px;padding:12px;margin-bottom:14px}
            small{color:#667085;font-weight:400}
            @media(max-width:420px){.to{grid-template-columns:1fr}}
            """);
        sb.Append("</style></head><body><div class=\"ramme\"><div class=\"kort\">");
        sb.Append("<h1>").Append(H(navn)).Append("</h1>");

        if (!i.SkjemaPa)
        {
            sb.Append("<p class=\"under\">Skjemaet er ikke i bruk. Ta kontakt med oss på telefon eller e-post.</p></div></div></body></html>");
            return sb.ToString();
        }

        if (melding is not null)
        {
            sb.Append("<div class=\"ok\"><b>Takk!</b><br>").Append(H(melding)).Append("</div></div></div></body></html>");
            return sb.ToString();
        }

        sb.Append("<p class=\"under\">Send oss en forespørsel, så tar vi kontakt.</p>");
        if (feil is not null)
        {
            sb.Append("<div class=\"feil\">").Append(H(feil)).Append("</div>");
        }

        sb.Append("<form method=\"post\" enctype=\"multipart/form-data\">");
        sb.Append("<input type=\"hidden\" name=\"t\" value=\"").Append(DateTime.UtcNow.Ticks).Append("\">");
        sb.Append("<div class=\"hp\" aria-hidden=\"true\"><label>Nettside<input name=\"nettside\" tabindex=\"-1\" autocomplete=\"off\"></label></div>");
        sb.Append("<div style=\"font-weight:600;font-size:.85rem\">Hva gjelder det?</div><div class=\"valg\">");
        var valg = Valg(i);
        for (var n = 0; n < valg.Length; n++)
        {
            var valgt = u.Kategori == valg[n] || (u.Kategori.Length == 0 && n == 0);
            sb.Append("<label><input type=\"radio\" name=\"kategori\" value=\"").Append(H(valg[n])).Append('"').Append(valgt ? " checked" : "").Append("><span>").Append(H(valg[n])).Append("</span></label>");
        }
        sb.Append("</div>");
        sb.Append("<label>Navn<input name=\"navn\" required maxlength=\"120\" autocomplete=\"name\" value=\"").Append(H(u.Navn)).Append("\"></label>");
        sb.Append("<div class=\"to\"><label>Telefon<input name=\"telefon\" type=\"tel\" maxlength=\"40\" autocomplete=\"tel\" value=\"").Append(H(u.Telefon)).Append("\"></label>");
        sb.Append("<label>E-post<input name=\"epost\" type=\"email\" maxlength=\"160\" autocomplete=\"email\" value=\"").Append(H(u.Epost)).Append("\"></label></div>");
        sb.Append("<label>Adresse <small>(der jobben skal gjøres)</small><input name=\"adresse\" maxlength=\"200\" autocomplete=\"street-address\" value=\"").Append(H(u.Adresse)).Append("\"></label>");
        sb.Append("<label>Beskriv kort hva det gjelder<textarea name=\"tekst\" required maxlength=\"4000\">").Append(H(u.Tekst)).Append("</textarea></label>");
        if (i.SkjemaBilder)
        {
            sb.Append("<label>Bilder <small>(valgfritt, inntil ").Append(MaksBilder).Append(")</small><input type=\"file\" name=\"bilder\" accept=\"image/*\" multiple></label>");
        }
        sb.Append("<button type=\"submit\">Send forespørsel</button></form>");
        sb.Append("<p class=\"under\" style=\"margin:14px 0 0;font-size:.8rem\">Opplysningene brukes bare til å svare på forespørselen din.</p>");
        sb.Append("</div></div></body></html>");
        return sb.ToString();
    }

    private static readonly HashSet<string> BildeTyper = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/heic", "image/heif"
    };

    public static bool ErBilde(string? contentType) => contentType is not null && BildeTyper.Contains(contentType);

    // Lagrer innsendingen som forespørsel i organisasjonens database.
    public static async Task<int> LagreAsync(ApplicationDbContext db, Utfylt u, IEnumerable<(string Filnavn, string ContentType, byte[] Data)> bilder)
    {
        var kontakt = !string.IsNullOrWhiteSpace(u.Epost) ? u.Epost.Trim() : u.Telefon.Trim();
        var tekst = u.Tekst.Trim();
        var emne = !string.IsNullOrWhiteSpace(u.Kategori) ? $"{u.Kategori.Trim()}: " : "";
        var forste = tekst.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        emne += forste.Length > 70 ? forste[..70].TrimEnd() + "…" : forste;

        var f = new Foresporsel
        {
            Kanal = ForesporselKanal.Skjema,
            FraNavn = u.Navn.Trim(),
            FraEpost = kontakt,
            Telefon = string.IsNullOrWhiteSpace(u.Telefon) ? null : u.Telefon.Trim(),
            Adresse = string.IsNullOrWhiteSpace(u.Adresse) ? null : u.Adresse.Trim(),
            Kategori = string.IsNullOrWhiteSpace(u.Kategori) ? null : u.Kategori.Trim(),
            Emne = emne.Length == 0 ? "Forespørsel fra nettskjema" : emne,
            Innhold = tekst,
            MottattDato = DateTime.Now,
            KundeId = await ForesporselInn.FinnKundeAsync(db, u.Epost)
        };
        foreach (var b in bilder)
        {
            f.Media.Add(new ForesporselMedia { Filnavn = b.Filnavn, ContentType = b.ContentType, Data = b.Data });
        }

        db.Foresporsler.Add(f);
        await db.SaveChangesAsync();
        return f.Id;
    }
}
