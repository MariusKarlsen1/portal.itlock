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

public class KoblingsSkjemaPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

    public async Task<byte[]?> GenerateAsync(int skjemaId)
    {
        var skjema = await db.KoblingsSkjemaer.Include(s => s.Prosjekt).FirstOrDefaultAsync(s => s.Id == skjemaId);
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

        var svg = BuildSvg(symboler, streker, bibliotek);
        var navngitte = symboler.Where(s => !string.IsNullOrWhiteSpace(s.Navn)).ToList();

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(headCol =>
                        {
                            headCol.Item().Text("Koblingsskjema").FontSize(18).SemiBold();
                            headCol.Item().Text(skjema.Navn).FontSize(11).FontColor(Colors.Grey.Darken1);
                        });
                        row.RelativeItem().AlignRight().Element(e => pdfLogo.Render(e, 16));
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text(t =>
                    {
                        t.Span("Kategori: ").Bold();
                        t.Span(skjema.Kategori.Visningsnavn());
                        t.Span("     Prosjekt: ").Bold();
                        t.Span(skjema.Prosjekt?.Navn ?? "-");
                        t.Span("     Dato: ").Bold();
                        t.Span(DateTime.Now.ToString("dd.MM.yyyy"));
                    });

                    col.Item().AlignCenter().Width(650).Height(360).Svg(svg).FitArea();

                    col.Item().Column(listCol =>
                    {
                        listCol.Item().Text("Symboler").Bold();
                        if (navngitte.Count == 0)
                        {
                            listCol.Item().Text("Ingen navngitte symboler.").FontColor(Colors.Grey.Darken1);
                        }

                        foreach (var s in navngitte)
                        {
                            listCol.Item().PaddingTop(2).Row(r =>
                            {
                                r.ConstantItem(10).Height(10).Background(Color.FromHex(s.Farge));
                                r.RelativeItem().PaddingLeft(6).Text($"{s.Navn} ({ElementTypeNavn(s.ElementType)})");
                            });
                        }
                    });
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Column(c =>
                {
                    c.Item().AlignCenter().Text($"{FirmaNavn} - {FirmaAdresse} - Tlf {FirmaTelefon} - {FirmaEpost}").FontSize(8);
                });
            });
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
        Dictionary<int, KoblingsSymbolBibliotek> bibliotek)
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

            var pts = string.Join(" ", punkter.Select(p => $"{Inv(p.X)},{Inv(YtilVb(p.Y))}"));
            var dashAttr = strek.Stiplet ? $"stroke-dasharray='{Inv(strek.Tykkelse * 0.5)},{Inv(strek.Tykkelse * 0.35)}'" : "";
            sb.Append(CultureInfo.InvariantCulture,
                $"<polyline points='{pts}' fill='none' stroke='{strek.Farge}' stroke-width='{Inv(strek.Tykkelse * 0.15)}' {dashAttr} />");

            if (!string.IsNullOrWhiteSpace(strek.Navn))
            {
                var midt = punkter[punkter.Count / 2];
                sb.Append(CultureInfo.InvariantCulture,
                    $"<text x='{Inv(midt.X)}' y='{Inv(YtilVb(midt.Y) - 1.5)}' font-size='2.4' font-family='Arial, sans-serif' text-anchor='middle' fill='{strek.Farge}'>{System.Net.WebUtility.HtmlEncode(strek.Navn)}</text>");
            }
        }

        foreach (var s in symboler)
        {
            var x = s.PosX;
            var y = YtilVb(s.PosY);
            var w = s.Bredde;
            var h = s.Hoyde * (ViewBoxHoyde / 100.0);
            var cx = x + w / 2;
            var cy = y + h / 2;

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

    private static double YtilVb(double prosentY) => prosentY * (ViewBoxHoyde / 100.0);

    private static string Inv(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
