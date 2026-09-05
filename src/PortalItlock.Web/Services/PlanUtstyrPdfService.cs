using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace PortalItlock.Web.Services;

public class PlanUtstyrPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";

    public async Task<byte[]?> GenerateAsync(int plantegningId)
    {
        var plantegning = await db.Plantegninger
            .Include(p => p.Prosjekt)
            .FirstOrDefaultAsync(p => p.Id == plantegningId);

        if (plantegning is null)
        {
            return null;
        }

        var utstyr = await db.PlanUtstyr
            .Where(u => u.PlantegningId == plantegningId)
            .ToListAsync();

        var utstyrIds = utstyr.Select(u => u.Id).ToList();

        var forbindelser = await db.PlanForbindelser
            .Where(f => utstyrIds.Contains(f.FraUtstyrId))
            .ToListAsync();

        var utstyrById = utstyr.ToDictionary(u => u.Id);

        using var bitmap = SKBitmap.Decode(plantegning.Data);
        var bildeBredde = bitmap?.Width ?? 1000;
        var bildeHoyde = bitmap?.Height ?? 700;

        var svg = BuildSvg(plantegning, utstyr, forbindelser, utstyrById, bildeBredde, bildeHoyde);
        var svgHoyde = 380f;
        var svgBredde = svgHoyde * bildeBredde / bildeHoyde;

        var brukteTyper = utstyr.Select(u => u.Type).Distinct().OrderBy(t => t.Visningsnavn()).ToList();
        var brukteKabler = forbindelser.Select(f => f.Type).Distinct().OrderBy(t => t.Visningsnavn()).ToList();

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
                            headCol.Item().Text("Utstyr og kabeltrekk").FontSize(18).SemiBold();
                            headCol.Item().Text(plantegning.Navn).FontSize(11).FontColor(Colors.Grey.Darken1);
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
                        t.Span("Prosjekt: ").Bold();
                        t.Span(plantegning.Prosjekt?.Navn ?? "");
                        t.Span("     Dato: ").Bold();
                        t.Span(DateTime.Now.ToString("dd.MM.yyyy"));
                    });

                    col.Item().AlignCenter().Width(svgBredde).Height(svgHoyde).Svg(svg).FitArea();

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(legendCol =>
                        {
                            legendCol.Item().Text("Utstyr").Bold();
                            if (brukteTyper.Count == 0)
                            {
                                legendCol.Item().Text("Ingen utstyr plassert.").FontColor(Colors.Grey.Darken1);
                            }
                            foreach (var type in brukteTyper)
                            {
                                legendCol.Item().PaddingTop(2).Row(r =>
                                {
                                    r.ConstantItem(10).Height(10).Background(Color.FromHex(type.Farge()));
                                    r.RelativeItem().PaddingLeft(6).Text($"{type.Kode()} - {type.Visningsnavn()}");
                                });
                            }
                        });

                        row.RelativeItem().Column(legendCol =>
                        {
                            legendCol.Item().Text("Kabling").Bold();
                            if (brukteKabler.Count == 0)
                            {
                                legendCol.Item().Text("Ingen forbindelser tegnet.").FontColor(Colors.Grey.Darken1);
                            }
                            foreach (var kt in brukteKabler)
                            {
                                legendCol.Item().PaddingTop(2).Row(r =>
                                {
                                    r.ConstantItem(10).Height(10).Background(Color.FromHex(kt.Farge()));
                                    r.RelativeItem().PaddingLeft(6).Text(kt.Visningsnavn());
                                });
                            }
                        });
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

    private static string BuildSvg(
        Plantegning plantegning,
        List<PlanUtstyr> utstyr,
        List<PlanForbindelse> forbindelser,
        Dictionary<int, PlanUtstyr> utstyrById,
        int bildeBredde,
        int bildeHoyde)
    {
        var base64 = Convert.ToBase64String(plantegning.Data);
        var diagonal = Math.Sqrt((double)bildeBredde * bildeBredde + (double)bildeHoyde * bildeHoyde);
        var radius = diagonal * 0.014;
        var strekbredde = diagonal * 0.0035;

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 {bildeBredde} {bildeHoyde}'>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<image xlink:href='data:{plantegning.ContentType};base64,{base64}' href='data:{plantegning.ContentType};base64,{base64}' x='0' y='0' width='{bildeBredde}' height='{bildeHoyde}' preserveAspectRatio='none' />");

        foreach (var f in forbindelser)
        {
            if (!utstyrById.TryGetValue(f.FraUtstyrId, out var fra) || !utstyrById.TryGetValue(f.TilUtstyrId, out var til))
            {
                continue;
            }

            var x1 = (fra.PosX / 100.0) * bildeBredde;
            var y1 = (fra.PosY / 100.0) * bildeHoyde;
            var x2 = (til.PosX / 100.0) * bildeBredde;
            var y2 = (til.PosY / 100.0) * bildeHoyde;
            var dash = SkalerStrekMonster(f.Type.StrekMonster(), diagonal);
            var dashAttr = dash is null ? "" : $"stroke-dasharray='{dash}'";

            sb.Append(CultureInfo.InvariantCulture,
                $"<line x1='{Inv(x1)}' y1='{Inv(y1)}' x2='{Inv(x2)}' y2='{Inv(y2)}' stroke='{f.Type.Farge()}' stroke-width='{Inv(strekbredde)}' {dashAttr} />");
        }

        var ikonStorrelse = diagonal * 0.02;
        var skala = ikonStorrelse / 24.0;
        var fontSize = diagonal * 0.0075;

        foreach (var u in utstyr)
        {
            var cx = (u.PosX / 100.0) * bildeBredde;
            var cy = (u.PosY / 100.0) * bildeHoyde;
            var x0 = cx - ikonStorrelse / 2;
            var y0 = cy - ikonStorrelse / 2;

            // Hvit "glorie" bak symbolet for kontrast mot tegningen, tilsvarer
            // drop-shadow-haloen rundt symbolet på skjermen.
            sb.Append(CultureInfo.InvariantCulture,
                $"<circle cx='{Inv(cx)}' cy='{Inv(cy)}' r='{Inv(ikonStorrelse * 0.62)}' fill='white' opacity='0.85' />");
            var ikonSvg = u.Type.IkonSvg().Replace("currentColor", u.Type.Farge());
            sb.Append(CultureInfo.InvariantCulture,
                $"<g transform='translate({Inv(x0)},{Inv(y0)}) scale({Inv(skala)})' stroke-linecap='round' stroke-linejoin='round'>{ikonSvg}</g>");

            AppendSentrertLabel(sb, cx, cy + ikonStorrelse / 2 + fontSize * 1.35, u.Type.Kode(), fontSize, "#292927", "white", "#00000026");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void AppendSentrertLabel(StringBuilder sb, double cx, double baselineY, string tekst, double fontSize, string tekstfarge, string bakgrunnsfarge, string? kantfarge)
    {
        if (string.IsNullOrEmpty(tekst))
        {
            return;
        }

        var bredde = tekst.Length * fontSize * 0.62 + fontSize * 0.6;
        var hoyde = fontSize * 1.4;
        var kantAttr = kantfarge is null ? "" : $"stroke='{kantfarge}' stroke-width='{Inv(fontSize * 0.06)}'";

        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x='{Inv(cx - bredde / 2)}' y='{Inv(baselineY - fontSize * 1.05)}' width='{Inv(bredde)}' height='{Inv(hoyde)}' fill='{bakgrunnsfarge}' {kantAttr} />");
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x='{Inv(cx)}' y='{Inv(baselineY)}' font-size='{Inv(fontSize)}' font-family='Arial, sans-serif' font-weight='bold' fill='{tekstfarge}' text-anchor='middle'>{System.Net.WebUtility.HtmlEncode(tekst)}</text>");
    }

    private static string? SkalerStrekMonster(string monster, double diagonal)
    {
        if (string.IsNullOrEmpty(monster))
        {
            return null;
        }

        var skala = diagonal * 0.0025;
        var deler = monster.Split(',').Select(p => Inv(double.Parse(p, CultureInfo.InvariantCulture) * skala));
        return string.Join(",", deler);
    }

    private static string Inv(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
