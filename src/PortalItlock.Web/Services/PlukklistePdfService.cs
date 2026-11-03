using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class PlukklistePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public async Task<List<LagretPdfLinje>> GetSnapshotLinjerAsync(int prosjektId, string? byggetrinn = null)
    {
        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var behov = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.LevertAv == LevertAv.F && k.ComponentId != 0
                && (valgteByggetrinn == null || (k.Dor!.Plantegning != null && valgteByggetrinn.Contains(k.Dor!.Plantegning!.Byggetrinn))))
            .Include(k => k.Component)
            .ToListAsync();

        return behov
            .Where(k => k.Component is not null)
            .GroupBy(k => k.Component!.Navn)
            .Select(g => new LagretPdfLinje(g.Key, g.Sum(x => x.Antall), null))
            .OrderBy(l => l.Navn)
            .ToList();
    }

    public async Task<byte[]?> GenerateAsync(int prosjektId, string? byggetrinn = null)
    {
        var prosjekt = await db.Prosjekter.FindAsync(prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var valgteByggetrinn = ByggetrinnHelper.ParseFilter(byggetrinn);

        var behov = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.LevertAv == LevertAv.F && k.ComponentId != 0
                && (valgteByggetrinn == null || (k.Dor!.Plantegning != null && valgteByggetrinn.Contains(k.Dor!.Plantegning!.Byggetrinn))))
            .Include(k => k.Component).ThenInclude(c => c!.Type)
            .ToListAsync();

        var gruppert = behov
            .Where(k => k.Component is not null)
            .GroupBy(k => k.ComponentId)
            .Select(g => new { ComponentId = g.Key, Component = g.First().Component!, Antall = g.Sum(x => x.Antall) })
            .OrderBy(x => x.Component.Type?.Navn)
            .ThenBy(x => x.Component.Navn)
            .ToList();

        if (gruppert.Count == 0)
        {
            return null;
        }

        var linjer = await db.PlukklisteLinjer
            .Where(p => p.ProsjektId == prosjektId)
            .ToListAsync();

        // P1 (liste-pdf-forslag.html): stående A4 i tilbudets stil - én liste nedover der det
        // som er plukket fylles inn for hånd, rute for OK og signatur nederst. Lager og
        // bestilling følges opp i lagermodulen og står ikke her.
        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = prosjekt.KundeId is { } kid ? await db.Kunder.FindAsync(kid) : null;
        var byggetrinnTekst = valgteByggetrinn is not null ? string.Join(", ", valgteByggetrinn) : "Alle";
        var plukketPer = linjer.GroupBy(l => l.ComponentId).ToDictionary(g => g.Key, g => g.Max(l => l.AntallPlukket));
        var sum = gruppert.Sum(g => g.Antall);

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                ListePdfStil.Side(page);
                ListePdfStil.Topp(page, org, pdfLogo, "Plukkliste", prosjekt.Navn);

                page.Content().PaddingTop(12).Column(col =>
                {
                    ListePdfStil.Info(col,
                        ("Kunde", kunde?.Navn ?? "–"),
                        ("Prosjekt", $"P-{prosjekt.Id}"),
                        ("Byggetrinn", byggetrinnTekst),
                        ("Utskrevet", DateTime.Now.ToString("dd.MM.yyyy")),
                        ("Varer / stk", $"{gruppert.Count} varer · {sum} stk"));

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);
                            c.RelativeColumn(4.2f);
                            c.RelativeColumn(1.7f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                            c.ConstantColumn(32);
                        });

                        table.Header(h =>
                        {
                            ListePdfStil.HodeTekst(h.Cell(), "#");
                            ListePdfStil.HodeTekst(h.Cell(), "Vare");
                            ListePdfStil.HodeTekst(h.Cell(), "Leverandør");
                            ListePdfStil.HodeTekst(h.Cell(), "Antall", hoyre: true);
                            ListePdfStil.HodeTekst(h.Cell(), "Plukket", midt: true);
                            ListePdfStil.HodeTekst(h.Cell(), "OK", midt: true);
                        });

                        for (var n = 0; n < gruppert.Count; n++)
                        {
                            var g = gruppert[n];
                            var zebra = n % 2 == 1;
                            var plukket = plukketPer.GetValueOrDefault(g.ComponentId);
                            ListePdfStil.Rad(table.Cell(), zebra).Text((n + 1).ToString()).FontSize(8).FontColor(ListePdfStil.Mut);
                            ListePdfStil.Vare(ListePdfStil.Rad(table.Cell(), zebra), g.Component.Navn, g.Component.Produktkode, g.Component.Type?.Navn);
                            ListePdfStil.Rad(table.Cell(), zebra).Text(g.Component.Leverandor ?? "").FontSize(8.5f);
                            ListePdfStil.Rad(table.Cell(), zebra).AlignRight().Text($"{g.Antall} {(g.Component.Enhet ?? "stk").ToLowerInvariant()}").FontSize(8.8f).Bold();
                            // Allerede plukket står med; ellers en strek å skrive på.
                            if (plukket > 0)
                            {
                                ListePdfStil.Rad(table.Cell(), zebra).AlignCenter().Text($"{Math.Min(plukket, g.Antall)}").FontSize(8.8f).Bold();
                            }
                            else
                            {
                                ListePdfStil.Rad(table.Cell(), zebra).AlignCenter().AlignBottom().Width(34).Height(11).BorderBottom(0.8f).BorderColor(Colors.Grey.Medium);
                            }
                            ListePdfStil.Rad(table.Cell(), zebra).AlignCenter().AlignMiddle().Width(10).Height(10).Border(0.8f).BorderColor(Colors.Grey.Medium)
                                .Background(plukket >= g.Antall ? ListePdfStil.LysGra : Colors.White)
                                .AlignCenter().AlignMiddle().Text(plukket >= g.Antall ? "✓" : "").FontSize(7).Bold();
                        }

                        ListePdfStil.SumRad(table.Cell()).Text("");
                        ListePdfStil.SumRad(table.Cell().ColumnSpan(2)).Text("Totalt").Bold();
                        ListePdfStil.SumRad(table.Cell()).AlignRight().Text($"{sum} stk").Bold();
                        ListePdfStil.SumRad(table.Cell().ColumnSpan(2)).Text("");
                    });

                    col.Item().PaddingTop(26).ShowEntire().Row(row =>
                    {
                        foreach (var felt in new[] { "Plukket av", "Dato", "Kontrollert av" })
                        {
                            row.RelativeItem().PaddingRight(14).BorderTop(0.8f).BorderColor(Colors.Grey.Medium).PaddingTop(3)
                                .Text(felt).FontSize(7.5f).FontColor(ListePdfStil.Mut);
                        }
                    });
                });

                ListePdfStil.Bunn(page, org, "Plukkliste");
            });
        });

        return document.GeneratePdf();
    }
}
