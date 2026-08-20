using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class LasplanPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    public async Task<byte[]?> GenerateAsync(int prosjektId)
    {
        var prosjekt = await db.Prosjekter.Include(p => p.Kunde).FirstOrDefaultAsync(p => p.Id == prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var nokler = await db.Nokler.Where(n => n.ProsjektId == prosjektId).OrderBy(n => n.Rekkefolge).ToListAsync();

        var dorKomponenter = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.Component!.ErSylinder)
            .Include(k => k.Dor)
            .Include(k => k.Component)
            .OrderBy(k => k.Dor!.Dornummer)
            .ToListAsync();

        if (dorKomponenter.Count == 0)
        {
            return null;
        }

        var rader = new List<(string Nummer, DorKomponent Dk)>();
        var nummer = 0;
        foreach (var dk in dorKomponenter)
        {
            nummer++;
            for (var i = 0; i < dk.Antall; i++)
            {
                rader.Add((i == 0 ? nummer.ToString() : $"{nummer}.{i}", dk));
            }
        }

        var nokkelIder = nokler.Select(n => n.Id).ToList();
        var krysser = await db.NokkelSylindere.Where(ns => nokkelIder.Contains(ns.NokkelId)).ToListAsync();
        var krysserSett = krysser.Select(k => (k.NokkelId, k.DorId, k.ComponentId)).ToHashSet();

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(e => pdfLogo.Render(e, 15));
                        row.RelativeItem().AlignRight().Text($"Låsplan – {prosjekt.Navn}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        void Felt(string label, string? verdi)
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(label).FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                                c.Item().Text(string.IsNullOrWhiteSpace(verdi) ? "–" : verdi).FontSize(9);
                            });
                        }

                        Felt("Låsplan systemnr.", prosjekt.LasplanSystemnr);
                        Felt("Systemeier", prosjekt.Kunde?.Navn);
                        Felt("Prosjektnummer", prosjekt.LasplanProsjektnummer);
                        Felt("Utarbeidet av/dato", prosjekt.LasplanUtarbeidetAv);
                    });
                });

                page.Content().PaddingTop(14).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(2.5f);
                        columns.ConstantColumn(28);
                        foreach (var _ in nokler)
                        {
                            columns.ConstantColumn(16);
                        }
                    });

                    table.Header(header =>
                    {
                        IContainer Hode() => header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).PaddingRight(2);

                        Hode().Text("Syl.mrk").Bold();
                        Hode().Text("Dør til").Bold();
                        Hode().Text("Dørnr.").Bold();
                        Hode().Text("Sylindertype").Bold();
                        Hode().Text("Ant.").Bold();

                        foreach (var n in nokler)
                        {
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).Height(90)
                                .RotateLeft().AlignMiddle()
                                .Text($"{n.Merking}: {n.Navn} ({n.Antall} stk)").FontSize(7).Bold();
                        }
                    });

                    foreach (var (visningsnummer, dk) in rader)
                    {
                        IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingRight(2);

                        Rad().Text(visningsnummer);
                        Rad().Text(dk.Dor!.DorTil ?? "");
                        Rad().Text(dk.Dor.Dornummer);
                        Rad().Text(dk.Component!.Navn);
                        Rad().Text("1");

                        foreach (var n in nokler)
                        {
                            var krysset = krysserSett.Contains((n.Id, dk.DorId, dk.ComponentId));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignCenter()
                                .Text(krysset ? "X" : "").Bold();
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
