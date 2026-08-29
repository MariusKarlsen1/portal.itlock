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

                page.Header().Row(row =>
                {
                    row.RelativeItem();
                    row.RelativeItem().AlignRight().Text($"Låsplan – {prosjekt.Navn}").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(8).Column(outerCol => outerCol.Item().Border(1).BorderColor(Colors.Black).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(110);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(2.5f);
                        columns.ConstantColumn(28);
                        if (nokler.Count > 0)
                        {
                            columns.ConstantColumn(30);
                        }
                        foreach (var _ in nokler)
                        {
                            columns.ConstantColumn(16);
                        }
                    });

                    table.Header(header =>
                    {
                        // Hver headerkolonne er lysegrå med kun topp/bunn-strek - ingen vertikale streker mellom dem, helt frem til nøkkelkolonnene.
                        IContainer Hode() => header.Cell().BorderTop(0.75f).BorderBottom(0.75f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).AlignBottom().PaddingVertical(2).PaddingHorizontal(2);

                        // Venstre "boks": logo øverst, systeminfo under, og "Syl.mrk" nederst - én egen, hvit firkant uten indre streker.
                        header.Cell().BorderTop(0.75f).BorderBottom(0.75f).BorderColor(Colors.Black).PaddingVertical(3).PaddingHorizontal(3).Column(c =>
                        {
                            c.Item().PaddingBottom(5).Element(e => pdfLogo.Render(e, 13));

                            void Felt(string label, string? verdi)
                            {
                                c.Item().PaddingTop(3).Text(label).FontSize(5.5f).Bold().FontColor(Colors.Grey.Darken2);
                                c.Item().Text(string.IsNullOrWhiteSpace(verdi) ? "–" : verdi).FontSize(7);
                            }

                            Felt("Låsplan systemnr.", prosjekt.LasplanSystemnr);
                            Felt("Systemeier", prosjekt.Kunde?.Navn);
                            Felt("Prosjektnummer", prosjekt.LasplanProsjektnummer);
                            Felt("Utarbeidet av/dato", prosjekt.LasplanUtarbeidetAv);

                            c.Item().PaddingTop(6).Text("Syl.mrk").Bold().FontSize(8);
                        });

                        Hode().Text("Dør til").Bold();
                        Hode().Text("Dørnr.").Bold();
                        Hode().Text("Sylindertype").Bold();
                        Hode().Text("Antall").Bold();

                        if (nokler.Count > 0)
                        {
                            // Forklarer de tre nivåene i nøkkel-kolonnene til høyre, med strek mellom hvert nivå.
                            // Alt som gjelder nøkler skrives på skrå (rotert), som i malen, i fet skrift lik resten av headeren.
                            header.Cell().BorderTop(0.75f).BorderBottom(0.75f).BorderLeft(0.75f).BorderRight(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).PaddingLeft(2).Column(c =>
                            {
                                c.Item().Height(20).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).RotateLeft().AlignMiddle().Text("Mrk.").FontSize(8).Bold();
                                c.Item().PaddingTop(1).Height(20).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).RotateLeft().AlignMiddle().Text("Ant.").FontSize(8).Bold();
                                c.Item().PaddingTop(1).Height(50).RotateLeft().AlignMiddle().Text("Nøkkelnavn").FontSize(8).Bold();
                            });
                        }

                        foreach (var n in nokler)
                        {
                            header.Cell().BorderTop(0.75f).BorderBottom(0.75f).BorderLeft(0.5f).BorderColor(Colors.Black).Column(c =>
                            {
                                c.Item().Height(20).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).RotateLeft().AlignMiddle().Text(n.Merking).FontSize(8).Bold();
                                c.Item().PaddingTop(1).Height(20).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1).RotateLeft().AlignMiddle().Text(n.Antall.ToString()).FontSize(8).Bold();
                                c.Item().PaddingTop(1).Height(50).RotateLeft().AlignMiddle().Text(n.Navn).FontSize(8).Bold();
                            });
                        }
                    });

                    foreach (var (visningsnummer, dk) in rader)
                    {
                        IContainer Rad() => table.Cell().Border(0.5f).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(2);

                        Rad().Text(visningsnummer);
                        Rad().Text(dk.Dor!.DorTil ?? "");
                        Rad().Text(dk.Dor.Dornummer);
                        Rad().Text(dk.Component!.Navn);
                        Rad().Text("1");

                        if (nokler.Count > 0)
                        {
                            table.Cell().Border(0.5f).BorderColor(Colors.Black);
                        }

                        foreach (var n in nokler)
                        {
                            var krysset = krysserSett.Contains((n.Id, dk.DorId, dk.ComponentId));
                            table.Cell().Border(0.5f).BorderColor(Colors.Black).AlignCenter()
                                .Text(krysset ? "X" : "").Bold();
                        }
                    }
                }));

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
