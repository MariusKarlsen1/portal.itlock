using System.Globalization;
using System.Net;
using System.Text;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Felles utseende for e-postene brukerne får: firmaets navn øverst, fargestripe
// og merkelapp for typen, overskrift, faktaboks, sitat, én knapp og bunntekst
// med firmaets opplysninger. Bygget med tabeller og inline-stil, så den ser lik
// ut i Outlook, Gmail og på mobil.
public static class EpostMal
{
    public static readonly CultureInfo Nb = CultureInfo.GetCultureInfo("nb-NO");

    public sealed record Farge(string Hoved, string Lys);

    public static readonly Farge Gronn = new("#3C6E55", "#E3EEE6");
    public static readonly Farge Bla = new("#3B5A80", "#E3EAF3");
    public static readonly Farge Brun = new("#8A5A34", "#F4ECE2");

    public sealed class Innhold
    {
        public required Farge Farge { get; init; }
        public required string Merke { get; init; }
        public required string Tittel { get; init; }
        public required string IntroHtml { get; init; }
        public List<(string Etikett, string VerdiHtml)> Fakta { get; init; } = [];
        public string? Sitat { get; init; }
        public required string KnappTekst { get; init; }
        public required string KnappUrl { get; init; }
        public string? Lenke2Tekst { get; init; }
        public string? Lenke2Url { get; init; }
        public string? Tips { get; init; }
        public required string Grunn { get; init; }
    }

    public static string E(string? tekst) => WebUtility.HtmlEncode(tekst ?? "");

    public static string Telefon(string? tlf) => string.IsNullOrWhiteSpace(tlf)
        ? ""
        : $"<a href=\"tel:{E(tlf.Replace(" ", ""))}\" style=\"color:#8A5A34;text-decoration:none;font-weight:bold\">{E(tlf)}</a>";

    public static string Epost(string? adresse) => string.IsNullOrWhiteSpace(adresse)
        ? ""
        : $"<a href=\"mailto:{E(adresse)}\" style=\"color:#8A5A34;text-decoration:none\">{E(adresse)}</a>";

    public static string KartUrl(string adresse) => "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(adresse);

    public static string Dato(DateTime d) => d.TimeOfDay == TimeSpan.Zero
        ? Stor(d.ToString("dddd d. MMMM", Nb))
        : Stor(d.ToString("dddd d. MMMM 'kl.' HH:mm", Nb));

    private static string Stor(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Nb) + s[1..];

    private static string Initialer(string navn)
    {
        var ord = navn.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(o => o is not ("AS" or "ASA" or "DA" or "ANS")).ToList();
        return ord.Count switch
        {
            0 => "?",
            1 => ord[0][..Math.Min(2, ord[0].Length)].ToUpperInvariant(),
            _ => $"{char.ToUpperInvariant(ord[0][0])}{char.ToUpperInvariant(ord[1][0])}"
        };
    }

    public static string Lag(Tenant org, Innhold i)
    {
        var navn = org.PortalVisningsnavn ?? org.Navn;
        var tema = OrganisasjonTema.FraId(org.Tema);
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"no\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"></head>");
        sb.Append("<body style=\"margin:0;padding:0;background:#F4F1EC\">");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F4F1EC\"><tr><td align=\"center\" style=\"padding:24px 12px\">");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:560px;font-family:Arial,Helvetica,sans-serif;color:#23201C\">");

        // Firmaets navn
        sb.Append("<tr><td style=\"padding:0 4px 14px\"><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\"><tr>");
        sb.Append($"<td style=\"width:34px;height:34px;border-radius:9px;background:{tema.Primaer};color:#ffffff;font-weight:bold;font-size:13px;text-align:center;vertical-align:middle\">{E(Initialer(navn))}</td>");
        sb.Append($"<td style=\"padding-left:10px;font-size:15px;font-weight:bold;color:#1A1712\">{E(navn)}</td>");
        sb.Append("</tr></table></td></tr>");

        // Kortet
        sb.Append("<tr><td style=\"background:#ffffff;border:1px solid #ECE7DD;border-radius:14px;overflow:hidden\">");
        sb.Append($"<div style=\"height:5px;background:{i.Farge.Hoved};border-radius:14px 14px 0 0\"></div>");
        sb.Append("<div style=\"padding:26px 28px 24px\">");
        sb.Append($"<span style=\"display:inline-block;font-size:11px;font-weight:bold;letter-spacing:.08em;text-transform:uppercase;padding:4px 10px;border-radius:99px;background:{i.Farge.Lys};color:{i.Farge.Hoved}\">{E(i.Merke)}</span>");
        sb.Append($"<h1 style=\"font-size:22px;line-height:1.3;color:#1A1712;margin:12px 0 8px;font-weight:bold\">{E(i.Tittel)}</h1>");
        sb.Append($"<div style=\"font-size:15px;line-height:1.6;color:#3F3A33;margin:0 0 14px\">{i.IntroHtml}</div>");

        if (i.Fakta.Count > 0)
        {
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border:1px solid #ECE7DD;border-radius:10px;margin:6px 0 20px;border-collapse:separate\">");
            for (var n = 0; n < i.Fakta.Count; n++)
            {
                var (etikett, verdi) = i.Fakta[n];
                var linje = n < i.Fakta.Count - 1 ? "border-bottom:1px solid #F1ECE3;" : "";
                sb.Append($"<tr><td style=\"{linje}padding:10px 14px;font-size:14px;color:#8A8176;width:110px;vertical-align:top\">{E(etikett)}</td>");
                sb.Append($"<td style=\"{linje}padding:10px 14px 10px 0;font-size:14px;color:#1A1712;font-weight:bold;vertical-align:top\">{verdi}</td></tr>");
            }

            sb.Append("</table>");
        }

        if (!string.IsNullOrWhiteSpace(i.Sitat))
        {
            sb.Append($"<div style=\"border-left:3px solid #D9B48A;background:#FBFAF7;padding:10px 14px;margin:0 0 20px;font-size:14px;color:#5C554B;line-height:1.55;white-space:pre-line\">{E(i.Sitat)}</div>");
        }

        sb.Append($"<a href=\"{E(i.KnappUrl)}\" style=\"display:inline-block;background:{i.Farge.Hoved};color:#ffffff;text-decoration:none;font-weight:bold;font-size:15px;padding:13px 22px;border-radius:9px\">{E(i.KnappTekst)}</a>");
        if (!string.IsNullOrWhiteSpace(i.Lenke2Tekst) && !string.IsNullOrWhiteSpace(i.Lenke2Url))
        {
            sb.Append($"<a href=\"{E(i.Lenke2Url)}\" style=\"display:inline-block;margin-left:14px;font-size:14px;color:#8A5A34;font-weight:bold;text-decoration:none\">{E(i.Lenke2Tekst)} &rarr;</a>");
        }

        if (!string.IsNullOrWhiteSpace(i.Tips))
        {
            sb.Append($"<div style=\"margin-top:22px;padding-top:16px;border-top:1px solid #F1ECE3;font-size:13px;color:#6E675C;line-height:1.55\">{E(i.Tips)}</div>");
        }

        sb.Append("</div></td></tr>");

        // Bunntekst
        var adresse = string.Join(", ", new[] { org.Adresse, $"{org.Postnr} {org.Sted}".Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var linjeEn = string.Join(" · ", new[] { navn, adresse, org.Telefon }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(E));
        sb.Append($"<tr><td style=\"padding:18px 10px 0;font-size:12px;line-height:1.6;color:#8A8176;text-align:center\">{linjeEn}<br>{E(i.Grunn)}");
        sb.Append("<div style=\"margin-top:10px;font-size:11px;color:#B3AA9C\">Levert av Full Kontroll</div></td></tr>");
        sb.Append("</table></td></tr></table></body></html>");
        return sb.ToString();
    }
}
