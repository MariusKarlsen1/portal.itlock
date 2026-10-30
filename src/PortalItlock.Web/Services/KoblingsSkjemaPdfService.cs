using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class KoblingsSkjemaPdfService(ApplicationDbContext db, PdfLogo pdfLogo, IWebHostEnvironment env)
{
    // Komponenttegninger (wwwroot/img/kobling) legges inn som innebygd SVG.
    // PNG-illustrasjoner pakkes inn i en SVG med bildet som data-URI.
    private string? LesTegning(string? sti)
    {
        if (sti is null || !sti.StartsWith("/img/kobling/", StringComparison.Ordinal))
        {
            return null;
        }
        var fil = Path.Combine(env.WebRootPath, sti.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fil))
        {
            return null;
        }
        if (!fil.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return File.ReadAllText(fil);
        }
        var data = File.ReadAllBytes(fil);
        if (data.Length < 24)
        {
            return null;
        }
        // Bredde og høyde står i IHDR-blokken (big-endian, byte 16-23).
        var w = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        var h = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        var b64 = Convert.ToBase64String(data);
        return $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 {w} {h}'>" +
               $"<image width='{w}' height='{h}' xlink:href='data:image/png;base64,{b64}' href='data:image/png;base64,{b64}' /></svg>";
    }

    public async Task<byte[]?> GenerateAsync(int skjemaId)
    {
        var skjema = await db.KoblingsSkjemaer.Include(s => s.Prosjekt).Include(s => s.Kategori).FirstOrDefaultAsync(s => s.Id == skjemaId);
        if (skjema is null)
        {
            return null;
        }

        var symboler = await db.KoblingsSymboler
            .Where(s => s.KoblingsSkjemaId == skjemaId)
            .OrderBy(s => s.ZIndex).ThenBy(s => s.Id)
            .ToListAsync();

        var streker = await db.KoblingsStreker
            .Where(s => s.KoblingsSkjemaId == skjemaId)
            .ToListAsync();

        var bibliotekIds = symboler
            .Where(s => s.SymbolBibliotekId.HasValue)
            .Select(s => s.SymbolBibliotekId!.Value)
            .Distinct()
            .ToList();

        var bibliotek = await db.KoblingsSymbolBibliotek
            .Where(b => bibliotekIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id);

        var svg = BuildSvg(symboler, streker, bibliotek, LesTegning);
        var navngitte = symboler.Where(s => !string.IsNullOrWhiteSpace(s.Navn)).ToList();

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));

                page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Koblingsskjema", skjema.Navn));

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(8);

                    PdfStil.InfoBoks(col, 3,
                        ("Kategori", skjema.Kategori?.Navn), ("Prosjekt", skjema.Prosjekt?.Navn),
                        ("Dato", DateTime.Now.ToString("dd.MM.yyyy")));

                    col.Item().AlignCenter().Width(650).Height(360).Svg(svg).FitArea();

                    col.Item().Column(listCol =>
                    {
                        PdfStil.SeksjonTittel(listCol, "Symboler");
                        if (navngitte.Count == 0)
                        {
                            listCol.Item().PaddingTop(3).Text("Ingen navngitte symboler.").FontSize(9).FontColor(Colors.Grey.Darken1);
                        }

                        foreach (var s in navngitte)
                        {
                            listCol.Item().PaddingTop(4).Row(r =>
                            {
                                r.ConstantItem(10).Height(10).Background(Color.FromHex(s.Farge));
                                r.RelativeItem().PaddingLeft(6).Text($"{s.Navn} ({ElementTypeNavn(s.ElementType)})").FontSize(9);
                            });
                        }
                    });
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
            });

            var kabler = streker.Where(s => s.ErKabel).OrderBy(s => s.Navn).ThenBy(s => s.Id).ToList();
            if (kabler.Count > 0)
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(PdfStil.Ink));
                    page.Header().Column(col => PdfStil.Header(col, pdfLogo, "Kabeljournal", skjema.Navn));
                    page.Content().PaddingTop(10).Column(col => Kabeljournal(col, kabler));
                    page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfStil.SandBorder).PaddingTop(6).Row(row => PdfStil.FooterRad(row));
                });
            }
        });

        return document.GeneratePdf();
    }

    private static string ElementTypeNavn(KoblingsElementType type) => type switch
    {
        KoblingsElementType.Bilde => "Bilde",
        KoblingsElementType.Rektangel => "Rektangel",
        KoblingsElementType.Sirkel => "Sirkel",
        KoblingsElementType.Linje => "Linje",
        KoblingsElementType.Pil => "Pil",
        KoblingsElementType.Tekstboks => "Tekstboks",
        KoblingsElementType.Punkt => "Punkt",
        _ => type.ToString()
    };

    private const double ViewBoxBredde = 100;
    private const double ViewBoxHoyde = 75;

    private static string BuildSvg(
        List<KoblingsSymbol> symboler,
        List<KoblingsStrek> streker,
        Dictionary<int, KoblingsSymbolBibliotek> bibliotek,
        Func<string?, string?> lesTegning)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 {ViewBoxBredde} {ViewBoxHoyde}'>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x='0' y='0' width='{ViewBoxBredde}' height='{ViewBoxHoyde}' fill='white' />");

        foreach (var strek in streker)
        {
            var punkter = JsonSerializer.Deserialize<List<KoblingsStrekPunkt>>(strek.PunkterJson) ?? [];
            if (punkter.Count == 0)
            {
                continue;
            }

            if (strek.ErKabel)
            {
                TegnKabel(sb, strek, punkter);
                continue;
            }

            var pts = string.Join(" ", punkter.Select(p => $"{Inv(p.X)},{Inv(YtilVb(p.Y))}"));
            var dashAttr = strek.Stiplet ? $"stroke-dasharray='{Inv(strek.Tykkelse * 0.5)},{Inv(strek.Tykkelse * 0.35)}'" : "";
            sb.Append(CultureInfo.InvariantCulture,
                $"<polyline points='{pts}' fill='none' stroke='{strek.Farge}' stroke-width='{Inv(strek.Tykkelse * 0.15)}' {dashAttr} />");
        }

        foreach (var s in symboler)
        {
            var x = s.PosX;
            var y = YtilVb(s.PosY);
            var w = s.Bredde;
            var h = s.Hoyde * (ViewBoxHoyde / 100.0);
            var cx = x + w / 2;
            var cy = y + h / 2;

            if (s.ElementType == KoblingsElementType.Komponent
                && lesTegning(KoblingsKomponentKatalog.Finn(s.KomponentNokkel)?.Bilde) is { } tegning)
            {
                // Bytt ut rot-elementets width/height med plassen på skjemaet.
                var start = tegning.IndexOf("<svg", StringComparison.Ordinal);
                var slutt = tegning.IndexOf('>', start);
                var rotTag = tegning[start..slutt];
                var viewBox = System.Text.RegularExpressions.Regex.Match(rotTag, "viewBox='([^']*)'").Groups[1].Value;
                sb.Append(CultureInfo.InvariantCulture,
                    $"<svg x='{Inv(x)}' y='{Inv(y)}' width='{Inv(w)}' height='{Inv(h)}' viewBox='{viewBox}' preserveAspectRatio='xMidYMid meet'>");
                sb.Append(tegning[(slutt + 1)..]);
            }

            switch (s.ElementType)
            {
                case KoblingsElementType.Bilde:
                    if (s.SymbolBibliotekId.HasValue && bibliotek.TryGetValue(s.SymbolBibliotekId.Value, out var bilde))
                    {
                        var base64 = Convert.ToBase64String(bilde.BildeData);
                        sb.Append(CultureInfo.InvariantCulture,
                            $"<image xlink:href='data:{bilde.BildeContentType};base64,{base64}' href='data:{bilde.BildeContentType};base64,{base64}' x='{Inv(x)}' y='{Inv(y)}' width='{Inv(w)}' height='{Inv(h)}' preserveAspectRatio='xMidYMid meet' />");
                    }
                    break;
                case KoblingsElementType.Rektangel:
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<rect x='{Inv(x)}' y='{Inv(y)}' width='{Inv(w)}' height='{Inv(h)}' fill='{(s.Fylt ? s.Farge : "none")}' stroke='{s.Farge}' stroke-width='{Inv(s.Strokbredde * 0.1)}' />");
                    break;
                case KoblingsElementType.Sirkel:
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<ellipse cx='{Inv(cx)}' cy='{Inv(cy)}' rx='{Inv(w / 2)}' ry='{Inv(h / 2)}' fill='{(s.Fylt ? s.Farge : "none")}' stroke='{s.Farge}' stroke-width='{Inv(s.Strokbredde * 0.1)}' />");
                    break;
                case KoblingsElementType.Linje:
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<line x1='{Inv(x)}' y1='{Inv(y)}' x2='{Inv(x + w)}' y2='{Inv(y + h)}' stroke='{s.Farge}' stroke-width='{Inv(s.Strokbredde * 0.1)}' />");
                    break;
                case KoblingsElementType.Pil:
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<line x1='{Inv(x)}' y1='{Inv(y)}' x2='{Inv(x + w)}' y2='{Inv(y + h)}' stroke='{s.Farge}' stroke-width='{Inv(s.Strokbredde * 0.1)}' />");
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<circle cx='{Inv(x + w)}' cy='{Inv(y + h)}' r='0.5' fill='{s.Farge}' />");
                    break;
                case KoblingsElementType.Tekstboks:
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<text x='{Inv(cx)}' y='{Inv(cy)}' font-size='{Inv(s.FontStorrelse * 0.1)}' font-family='Arial, sans-serif' text-anchor='middle' fill='{s.Farge}'>{System.Net.WebUtility.HtmlEncode(s.Tekst ?? "")}</text>");
                    break;
                case KoblingsElementType.Punkt:
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<circle cx='{Inv(cx)}' cy='{Inv(cy)}' r='{Inv(Math.Min(w, h) / 2)}' fill='{s.Farge}' stroke='white' stroke-width='0.2' />");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(s.Navn) && s.ElementType != KoblingsElementType.Tekstboks)
            {
                sb.Append(CultureInfo.InvariantCulture,
                    $"<text x='{Inv(cx)}' y='{Inv(y - 0.8)}' font-size='2.2' font-family='Arial, sans-serif' text-anchor='middle' fill='black'>{System.Net.WebUtility.HtmlEncode(s.Navn)}</text>");
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    // Kabel med ledere: tykk sort kabel som deler seg i fargede ledere i hver ende.
    private static void TegnKabel(StringBuilder sb, KoblingsStrek strek, List<KoblingsStrekPunkt> punkter)
    {
        var vifte = KoblingsKabel.LagVifte(punkter, KoblingsKabel.Les(strek.LedereJson), ViewBoxHoyde / ViewBoxBredde, strek.LederAvstand);
        if (vifte is null)
        {
            return;
        }

        static string Pts(IEnumerable<KoblingsStrekPunkt> p) => string.Join(" ", p.Select(q => $"{Inv(q.X)},{Inv(YtilVb(q.Y))}"));

        sb.Append(CultureInfo.InvariantCulture,
            $"<polyline points='{Pts(vifte.Kropp)}' fill='none' stroke='{KoblingsKabel.KabelFarge}' stroke-width='0.9' stroke-linejoin='round' stroke-linecap='round' />");
        foreach (var l in vifte.Ledere)
        {
            var pts = Pts(l.Punkter);
            if (l.Leder.Farge == "Hvit")
            {
                sb.Append(CultureInfo.InvariantCulture,
                    $"<polyline points='{pts}' fill='none' stroke='#9a948c' stroke-width='0.5' stroke-linejoin='round' stroke-linecap='round' />");
            }
            var dash = l.Leder.Stiplet ? "stroke-dasharray='0.6,0.45'" : "";
            sb.Append(CultureInfo.InvariantCulture,
                $"<polyline points='{pts}' fill='none' stroke='{KoblingsKabel.Hex(l.Leder.Farge)}' stroke-width='0.32' stroke-linejoin='round' stroke-linecap='round' {dash} />");
        }

        // Navnet på hver leder ved enden, som på skjermen.
        var fs = Math.Clamp(vifte.Mellom * 0.82, 0.45, 1.1);
        foreach (var l in vifte.Ledere)
        {
            if (KoblingsKabel.LagEtikett(l, ViewBoxHoyde / ViewBoxBredde) is not { } et)
            {
                continue;
            }
            var dx = et.Etter ? fs * 0.45 : -fs * 0.6;
            sb.Append(CultureInfo.InvariantCulture,
                $"<text transform='translate({Inv(et.Punkt.X)},{Inv(YtilVb(et.Punkt.Y))}) rotate({Inv(et.Vinkel)})' x='{Inv(dx)}' y='{Inv(fs * 0.35)}' font-size='{Inv(fs)}' font-family='Arial, sans-serif' font-weight='bold' text-anchor='{(et.Etter ? "start" : "end")}' fill='#2a2622'>{System.Net.WebUtility.HtmlEncode(KoblingsKabel.LederNavn(l.Leder))}</text>");
        }

        if (!string.IsNullOrWhiteSpace(strek.Navn))
        {
            var etikett = $"{strek.Navn} · {(string.IsNullOrWhiteSpace(strek.KabelType) ? KoblingsKabel.MalNavn(KoblingsKabel.Les(strek.LedereJson)) : strek.KabelType)}";
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x='{Inv(vifte.Merke.X)}' y='{Inv(YtilVb(vifte.Merke.Y) - 1.1)}' font-size='1.3' font-weight='bold' font-family='Arial, sans-serif' text-anchor='middle' fill='#2a2622'>{System.Net.WebUtility.HtmlEncode(etikett)}</text>");
        }
    }

    // Kabeljournal: én rad per leder - klemme, funksjon, leder, hvor den skal og merknad.
    private static void Kabeljournal(ColumnDescriptor col, List<KoblingsStrek> kabler)
    {
        PdfStil.SeksjonTittel(col, "Kabeljournal");
        col.Item().PaddingBottom(2).Text("Én rad = én leder. Les fra venstre: klemme → leder → hvor den skal.").FontSize(8.5f).FontColor(Colors.Grey.Darken1);

        foreach (var k in kabler)
        {
            var ledere = KoblingsKabel.Les(k.LedereJson);
            var tittel = string.IsNullOrWhiteSpace(k.Navn) ? "Kabel" : k.Navn;
            var type = string.IsNullOrWhiteSpace(k.KabelType) ? KoblingsKabel.MalNavn(ledere) : $"{k.KabelType} · {KoblingsKabel.MalNavn(ledere)}";

            col.Item().PaddingTop(6).Text(t =>
            {
                t.Span(tittel).Bold().FontSize(10);
                t.Span("  " + type).FontSize(9).FontColor(Colors.Grey.Darken1);
            });

            col.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(52);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2.4f);
                    c.RelativeColumn(3);
                    c.RelativeColumn(3);
                });
                table.Header(h =>
                {
                    foreach (var tekst in new[] { "Klemme", "Funksjon", "Leder", "Kobles til", "Merknad" })
                    {
                        h.Cell().Element(PdfStil.TabellHode).Text(tekst).FontColor(Colors.White).Bold().FontSize(8.5f);
                    }
                });
                for (var i = 0; i < ledere.Count; i++)
                {
                    var l = ledere[i];
                    var alt = i % 2 == 1;
                    table.Cell().Element(c => PdfStil.TabellRad(c, alt)).Text(string.IsNullOrWhiteSpace(l.Klemme) ? "–" : l.Klemme).Bold();
                    table.Cell().Element(c => PdfStil.TabellRad(c, alt)).Text(l.Funksjon);
                    table.Cell().Element(c => PdfStil.TabellRad(c, alt)).Row(r =>
                    {
                        r.ConstantItem(8).AlignMiddle().Height(8).Background(Color.FromHex(KoblingsKabel.Hex(l.Farge))).Border(0.5f).BorderColor(Colors.Grey.Medium);
                        r.RelativeItem().PaddingLeft(5).Text(KoblingsKabel.LederTekst(l));
                    });
                    table.Cell().Element(c => PdfStil.TabellRad(c, alt)).Text(l.Til);
                    table.Cell().Element(c => PdfStil.TabellRad(c, alt)).Text(l.Merknad).FontColor(Colors.Grey.Darken2);
                }
            });
        }
    }

    private static double YtilVb(double prosentY) => prosentY * (ViewBoxHoyde / 100.0);

    private static string Inv(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
