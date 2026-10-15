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

public class PlanUtstyrPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{

    public async Task<byte[]?> GenerateAsync(int plantegningId)
    {
        var plantegning = await db.Plantegninger
            .Include(p => p.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(p => p.Fil)
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
        var brukteTyper = utstyr.GroupBy(u => u.Type).OrderBy(g => g.Key.Visningsnavn()).ToList();
        var brukteKabler = forbindelser.Select(f => f.Type).Distinct().OrderBy(t => t.Visningsnavn()).ToList();

        // Mal U1: se PlantegningPdfMal. Forklaringen viser utstyret som er brukt
        // (med antall) og kabeltypene med samme strek som på tegningen.
        var org = RapportProfil.Fra(tenantContext.Current);
        const string tittel = "Utstyr og kabeltrekk";

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                PlantegningPdfMal.Side(page);
                page.Header().Element(e => PlantegningPdfMal.Topp(e, org, pdfLogo, tittel, plantegning));
                page.Content().Element(e => PlantegningPdfMal.Tegning(e, svg));
                page.Footer().Element(e => PlantegningPdfMal.Bunn(e, org, tittel, plantegning, col =>
                {
                    if (brukteTyper.Count == 0)
                    {
                        col.Item().Text("Ingen utstyr plassert.").FontSize(7.5f).FontColor(PlantegningPdfMal.Dempet);
                    }
                    else
                    {
                        PlantegningPdfMal.ForklaringRad(col, brukteTyper.Select(g => (Action<IContainer>)(c => c.Row(r =>
                        {
                            r.AutoItem().AlignMiddle().MinWidth(15).Height(9).CornerRadius(2).Background(Color.FromHex(g.Key.Farge()))
                                .PaddingHorizontal(2).AlignCenter().AlignMiddle().Text(g.Key.Kode()).FontSize(5).Bold().FontColor(Colors.White);
                            r.AutoItem().PaddingLeft(4).Text(t =>
                            {
                                t.Span(g.Key.Visningsnavn()).FontSize(7.5f);
                                t.Span($"  {g.Count()}").FontSize(7.5f).Bold();
                            });
                        }))));
                    }

                    if (brukteKabler.Count > 0)
                    {
                        col.Item().PaddingTop(3).Column(kabelCol => PlantegningPdfMal.ForklaringRad(kabelCol, brukteKabler.Select(kt => (Action<IContainer>)(c => c.Row(r =>
                        {
                            var strek = kt.StrekMonster();
                            var dash = string.IsNullOrEmpty(strek) ? "" : $" stroke-dasharray='{strek}'";
                            r.AutoItem().AlignMiddle().Width(20).Height(6)
                                .Svg($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 6'><line x1='0' y1='3' x2='20' y2='3' stroke='{kt.Farge()}' stroke-width='1.6'{dash} /></svg>");
                            r.AutoItem().PaddingLeft(4).Text(kt.Visningsnavn()).FontSize(7.5f);
                        })))));
                    }
                }));
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
        var strekbredde = diagonal * 0.0035;

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 {bildeBredde} {bildeHoyde}'>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<image xlink:href='data:{plantegning.ContentType};base64,{base64}' href='data:{plantegning.ContentType};base64,{base64}' x='0' y='0' width='{bildeBredde}' height='{bildeHoyde}' preserveAspectRatio='none' />");

        var ikonStorrelse = diagonal * 0.013;
        var skala = ikonStorrelse / 24.0;
        var fontSize = diagonal * 0.005;
        var haloRadius = ikonStorrelse * 0.62;

        // Kabler tegnes over gloriene, men under selve symbolene, slik at
        // kabelen alltid er synlig helt inn til utstyret - selv når to
        // enheter står tett og gloriene deres ellers ville dekket streken.
        foreach (var u in utstyr)
        {
            var cx = (u.PosX / 100.0) * bildeBredde;
            var cy = (u.PosY / 100.0) * bildeHoyde;

            // Hvit "glorie" bak symbolet for kontrast mot tegningen, tilsvarer
            // drop-shadow-haloen rundt symbolet på skjermen.
            sb.Append(CultureInfo.InvariantCulture,
                $"<circle cx='{Inv(cx)}' cy='{Inv(cy)}' r='{Inv(haloRadius)}' fill='white' opacity='0.85' />");
        }

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

        foreach (var u in utstyr)
        {
            var cx = (u.PosX / 100.0) * bildeBredde;
            var cy = (u.PosY / 100.0) * bildeHoyde;
            var x0 = cx - ikonStorrelse / 2;
            var y0 = cy - ikonStorrelse / 2;

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
