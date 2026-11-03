using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class ProduktsammendragPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public async Task<byte[]?> GenerateAsync(int prosjektId, string? byggetrinn = null)
    {
        var prosjekt = await db.Prosjekter.FindAsync(prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);
        var komponenter = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.ComponentId != 0
                && (valgteByggetrinn == null || (k.Dor!.Plantegning != null && valgteByggetrinn.Contains(k.Dor!.Plantegning!.Byggetrinn))))
            .Include(k => k.Component).ThenInclude(c => c!.Type)
            .ToListAsync();

        var rader = komponenter
            .Where(k => k.Component is not null)
            .GroupBy(k => new { k.ComponentId, k.LevertAv })
            .Select(g => new
            {
                Component = g.First().Component!,
                g.Key.LevertAv,
                Antall = g.Sum(x => x.Antall)
            })
            .OrderBy(r => r.Component.Type?.Navn)
            .ThenBy(r => r.Component.Navn)
            .ToList();

        if (rader.Count == 0)
        {
            return null;
        }

        // S1 (liste-pdf-forslag.html): som siden - vare med varenr under, beslagstype,
        // overflate, levering skrevet ut og antall med enhet, og totalt nederst.
        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = prosjekt.KundeId is { } kid ? await db.Kunder.FindAsync(kid) : null;
        var antallDorer = komponenter.Select(k => k.DorId).Distinct().Count();
        var sum = rader.Sum(r => r.Antall);

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                ListePdfStil.Side(page);
                ListePdfStil.Topp(page, org, pdfLogo, "Produktsammendrag", prosjekt.Navn);

                page.Content().PaddingTop(12).Column(col =>
                {
                    ListePdfStil.Info(col,
                        ("Kunde", kunde?.Navn ?? "–"),
                        ("Byggetrinn", valgteByggetrinn is not null ? string.Join(", ", valgteByggetrinn) : "Alle"),
                        ("Utskrevet", DateTime.Now.ToString("dd.MM.yyyy")),
                        ("Varer / stk", $"{rader.Select(r => r.Component.Id).Distinct().Count()} varer · {sum} stk"),
                        ("Dører", antallDorer.ToString()));

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4f);
                            c.RelativeColumn(1.9f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn(1f);
                        });

                        table.Header(h =>
                        {
                            ListePdfStil.HodeTekst(h.Cell(), "Vare");
                            ListePdfStil.HodeTekst(h.Cell(), "Beslagstype");
                            ListePdfStil.HodeTekst(h.Cell(), "Overflate");
                            ListePdfStil.HodeTekst(h.Cell(), "Levering");
                            ListePdfStil.HodeTekst(h.Cell(), "Antall", hoyre: true);
                        });

                        for (var n = 0; n < rader.Count; n++)
                        {
                            var r = rader[n];
                            var zebra = n % 2 == 1;
                            ListePdfStil.Vare(ListePdfStil.Rad(table.Cell(), zebra), r.Component.Navn, r.Component.Produktkode);
                            ListePdfStil.Rad(table.Cell(), zebra).Text(r.Component.Type?.Navn ?? "–").FontSize(8.5f);
                            ListePdfStil.Rad(table.Cell(), zebra).Text(string.IsNullOrWhiteSpace(r.Component.Overflate) ? "–" : r.Component.Overflate).FontSize(8.5f).FontColor(ListePdfStil.Mut);
                            ListePdfStil.Rad(table.Cell(), zebra).Text(ListePdfStil.Levering(r.LevertAv)).FontSize(8.5f);
                            ListePdfStil.Rad(table.Cell(), zebra).AlignRight().Text($"{r.Antall} {(r.Component.Enhet ?? "stk").ToLowerInvariant()}").FontSize(8.8f).Bold();
                        }

                        ListePdfStil.SumRad(table.Cell().ColumnSpan(4)).Text("Totalt").Bold();
                        ListePdfStil.SumRad(table.Cell()).AlignRight().Text($"{sum} stk").Bold();
                    });
                });

                ListePdfStil.Bunn(page, org, "Produktsammendrag");
            });
        });

        return document.GeneratePdf();
    }
}
