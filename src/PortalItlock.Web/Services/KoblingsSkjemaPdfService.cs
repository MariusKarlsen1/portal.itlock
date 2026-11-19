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
        if (KoblingsTilpasning.TilpassSkjema(symboler, streker))
        {
            await db.SaveChangesAsync();
        }

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

                    col.Item().AlignCenter().Height(400).Svg(svg).FitArea();

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
    private const double ViewBoxHoyde = 81.25;

    private static string BuildSvg(
        List<KoblingsSymbol> symboler,
        List<KoblingsStrek> streker,
        Dictionary<int, KoblingsSymbolBibliotek> bibliotek,
        Func<string?, string?> lesTegning)
    {
        var (vx, vy, vb, vh) = Utsnitt(symboler, streker);
        // Tekst måles i enheter av utsnittet: ca. 7 pt når tegningen fyller ~770 pt.
        var navnStr = vb * 7.0 / 770;
        // Strekene blir like tynne på arket uansett hvor mye tegningen forstørres.
        var f = vb / ViewBoxBredde;
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='{Inv(vx)} {Inv(vy)} {Inv(vb)} {Inv(vh)}'>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x='{Inv(vx)}' y='{Inv(vy)}' width='{Inv(vb)}' height='{Inv(vh)}' fill='white' />");
        var etiketter = new List<(double X0, double Y0, double X1, double Y1)>();
        bool Ledig(double cx, double grunnlinje, string tekst, double str)
        {
            var halv = tekst.Length * str * 0.3;
            var r = (cx - halv, grunnlinje - str, cx + halv, grunnlinje + str * 0.2);
            if (etiketter.Any(e => r.Item1 < e.X1 && r.Item3 > e.X0 && r.Item2 < e.Y1 && r.Item4 > e.Y0))
            {
                return false; // ville gått over en annen tekst: utelates
            }
            etiketter.Add(r);
            return true;
        }

        var strekStart = sb.Length;
        foreach (var strek in streker)
        {
            var punkter = JsonSerializer.Deserialize<List<KoblingsStrekPunkt>>(strek.PunkterJson) ?? [];
            if (punkter.Count == 0)
            {
                continue;
            }

            if (strek.ErKabel)
            {
                TegnKabel(sb, strek, punkter, f);
                continue;
            }

            foreach (var t in KoblingsStrekTekst.For(strek, punkter, ViewBoxHoyde / ViewBoxBredde))
            {
                var ty = YtilVb(t.Y);
                var (tx, anker, gy) = t.Klasse switch
                {
                    "ks-se-hoyre" => (t.X - navnStr * 0.4, "end", ty + navnStr * 0.35),
                    "ks-se-venstre" => (t.X + navnStr * 0.4, "start", ty + navnStr * 0.35),
                    "ks-se-ned" => (t.X, "middle", ty - navnStr * 0.4),
                    "ks-se-opp" => (t.X, "middle", ty + navnStr * 1.1),
                    "ks-st-venstre" => (t.X - navnStr * 0.6, "end", ty - navnStr * 0.3),
                    "ks-st-ned" => (t.X + navnStr * 0.4, "start", ty + navnStr * 1.2),
                    "ks-st-opp" => (t.X + navnStr * 0.4, "start", ty - navnStr * 0.5),
                    _ => (t.X + navnStr * 0.6, "start", ty - navnStr * 0.3)
                };
                if (Ledig(tx + (anker == "end" ? -1 : anker == "middle" ? 0 : 1) * t.Tekst.Length * navnStr * 0.3, gy, t.Tekst, navnStr))
                {
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<text x='{Inv(tx)}' y='{Inv(gy)}' font-size='{Inv(navnStr)}' font-weight='bold' font-family='Arial, sans-serif' text-anchor='{anker}' fill='{KoblingsStrekTekst.Farge(strek)}'{(KoblingsStrekTekst.ErHvit(strek.Farge) ? $" stroke='#fff' stroke-width='{Inv(navnStr * 0.25)}' paint-order='stroke'" : "")}>{System.Net.WebUtility.HtmlEncode(t.Tekst)}</text>");
                }
            }

            var pts = string.Join(" ", punkter.Select(p => $"{Inv(p.X)},{Inv(YtilVb(p.Y))}"));
            var dashAttr = strek.Stiplet ? $"stroke-dasharray='{Inv(strek.Tykkelse * 0.5 * f)},{Inv(strek.Tykkelse * 0.35 * f)}'" : "";
            if (KoblingsStrekTekst.ErHvit(strek.Farge))
            {
                sb.Append(CultureInfo.InvariantCulture,
                    $"<polyline points='{pts}' fill='none' stroke='{KoblingsStrekTekst.HvitKant}' stroke-width='{Inv((strek.Tykkelse * 0.15 + 0.25) * f)}' stroke-linejoin='round' />");
            }
            sb.Append(CultureInfo.InvariantCulture,
                $"<polyline points='{pts}' fill='none' stroke='{strek.Farge}' stroke-width='{Inv(strek.Tykkelse * 0.15 * f)}' {dashAttr} />");
        }

        var strekSvg = sb.ToString(strekStart, sb.Length - strekStart);

        // Halvmåne der streker krysser, prikk der de er koblet - som på skjermen.
        var pdfAspekt = ViewBoxHoyde / ViewBoxBredde;
        sb.Append(KoblingsKryss.Svg(
            KoblingsKryss.Linjer(streker,
                s => KoblingsKabel.LagVifte(JsonSerializer.Deserialize<List<KoblingsStrekPunkt>>(s.PunkterJson) ?? [], KoblingsKabel.Les(s.LedereJson), pdfAspekt, s.LederAvstand, s.LederLengde, s.KabelMerkePlass),
                j => JsonSerializer.Deserialize<List<KoblingsStrekPunkt>>(j) ?? []),
            pdfAspekt, yFaktor: ViewBoxHoyde / 100.0, breddeFaktor: 0.14 * f, ikkeSkalerende: false, pxPerProsent: 1 / (0.14 * f)));

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
                        $"<text x='{Inv(cx)}' y='{Inv(cy + s.FontStorrelse / 12.0 * 0.35)}' font-size='{Inv(s.FontStorrelse / 12.0)}' font-family='Arial, sans-serif' text-anchor='middle'{(s.Fet ? " font-weight='bold'" : "")} fill='{s.Farge}'>{System.Net.WebUtility.HtmlEncode(s.Tekst ?? "")}</text>");
                    break;
                case KoblingsElementType.Punkt:
                    sb.Append(CultureInfo.InvariantCulture,
                        $"<circle cx='{Inv(cx)}' cy='{Inv(cy)}' r='{Inv(Math.Min(w, h) / 2)}' fill='{s.Farge}' />");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(s.Navn) && !s.SkjulNavn && s.ElementType is not (KoblingsElementType.Tekstboks or KoblingsElementType.Punkt)
                && Ledig(cx, y - navnStr * 0.4, s.Navn, navnStr))
            {
                sb.Append(CultureInfo.InvariantCulture,
                    $"<text x='{Inv(cx)}' y='{Inv(y - navnStr * 0.4)}' font-size='{Inv(navnStr)}' font-weight='bold' font-family='Arial, sans-serif' text-anchor='middle' fill='#14213D'>{System.Net.WebUtility.HtmlEncode(s.Navn)}</text>");
            }
        }

        // Kabler som skal vises helt inn til punktet: tegnes på nytt oppå de komponentene.
        var over = symboler.Where(s => s.KablerOver).ToList();
        if (over.Count > 0)
        {
            sb.Append("<defs><clipPath id='kabler-over'>");
            foreach (var k in over)
            {
                sb.Append(CultureInfo.InvariantCulture,
                    $"<rect x='{Inv(k.PosX)}' y='{Inv(YtilVb(k.PosY))}' width='{Inv(k.Bredde)}' height='{Inv(k.Hoyde * (ViewBoxHoyde / 100.0))}' />");
            }
            sb.Append("</clipPath></defs><g clip-path='url(#kabler-over)'>").Append(strekSvg).Append("</g>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    // Utsnittet rundt alt som er tegnet (i viewBox-enheter), med litt luft.
    private static (double X, double Y, double B, double H) Utsnitt(List<KoblingsSymbol> symboler, List<KoblingsStrek> streker)
    {
        var xs = new List<double>();
        var ys = new List<double>();
        foreach (var s in symboler)
        {
            xs.Add(s.PosX);
            xs.Add(s.PosX + s.Bredde);
            ys.Add(YtilVb(s.PosY) - 2.5);
            ys.Add(YtilVb(s.PosY + s.Hoyde));
        }
        foreach (var p in streker.SelectMany(st => JsonSerializer.Deserialize<List<KoblingsStrekPunkt>>(st.PunkterJson) ?? []))
        {
            xs.Add(p.X);
            ys.Add(YtilVb(p.Y));
        }
        if (xs.Count == 0)
        {
            return (0, 0, ViewBoxBredde, ViewBoxHoyde);
        }
        const double luft = 2;
        double x0 = xs.Min() - luft, x1 = xs.Max() + luft, y0 = ys.Min() - luft, y1 = ys.Max() + luft;
        return (x0, y0, Math.Max(10, x1 - x0), Math.Max(8, y1 - y0));
    }

    // Kabel med ledere: tykk sort kabel som deler seg i fargede ledere i hver ende.
    private static void TegnKabel(StringBuilder sb, KoblingsStrek strek, List<KoblingsStrekPunkt> punkter, double f)
    {
        var vifte = KoblingsKabel.LagVifte(punkter, KoblingsKabel.Les(strek.LedereJson), ViewBoxHoyde / ViewBoxBredde, strek.LederAvstand, strek.LederLengde, strek.KabelMerkePlass);
        if (vifte is null)
        {
            return;
        }

        static string Pts(IEnumerable<KoblingsStrekPunkt> p) => string.Join(" ", p.Select(q => $"{Inv(q.X)},{Inv(YtilVb(q.Y))}"));

        sb.Append(CultureInfo.InvariantCulture,
            $"<polyline points='{Pts(vifte.Kropp)}' fill='none' stroke='{KoblingsKabel.KabelFarge}' stroke-width='{Inv(0.9 * f)}' stroke-linejoin='round' stroke-linecap='round' />");
        foreach (var l in vifte.Ledere)
        {
            var pts = Pts(l.Punkter);
            if (l.Leder.Farge == "Hvit")
            {
                sb.Append(CultureInfo.InvariantCulture,
                    $"<polyline points='{pts}' fill='none' stroke='#9a948c' stroke-width='{Inv(0.5 * f)}' stroke-linejoin='round' stroke-linecap='round' />");
            }
            var dash = l.Leder.Stiplet ? "stroke-dasharray='0.6,0.45'" : "";
            sb.Append(CultureInfo.InvariantCulture,
                $"<polyline points='{pts}' fill='none' stroke='{KoblingsKabel.Hex(l.Leder.Farge)}' stroke-width='{Inv(0.32 * f)}' stroke-linejoin='round' stroke-linecap='round' {dash} />");
        }

        if (!string.IsNullOrWhiteSpace(strek.Navn))
        {
            var etikett = $"{strek.Navn} · {(string.IsNullOrWhiteSpace(strek.KabelType) ? KoblingsKabel.MalNavn(KoblingsKabel.Les(strek.LedereJson)) : strek.KabelType)}";
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x='{Inv(vifte.Merke.X)}' y='{Inv(YtilVb(vifte.Merke.Y) - 1.1)}' font-size='{Inv(1.3 * f)}' font-weight='bold' font-family='Arial, sans-serif' text-anchor='middle' fill='#2a2622'>{System.Net.WebUtility.HtmlEncode(etikett)}</text>");
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

    private static string Inv(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
