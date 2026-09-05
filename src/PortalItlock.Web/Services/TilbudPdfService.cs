using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class TilbudPdfService(ApplicationDbContext db, PdfLogo pdfLogo)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    private const string FirmaNavn = "ITLOCK AS";
    private const string FirmaAdresse = "Gartnerveien 2, 4374 Egersund";
    private const string FirmaTelefon = "47355441";
    private const string FirmaEpost = "marius@itlock.no";
    private const string FirmaKontaktperson = "Marius Karlsen";
    private const decimal MvaSats = 0.25m;

    public async Task<List<LagretPdfLinje>?> GetSnapshotLinjerAsync(int tilbudId)
    {
        var tilbud = await db.Tilbud
            .Include(t => t.Linjer).ThenInclude(l => l.Component).ThenInclude(c => c!.Type)
            .FirstOrDefaultAsync(t => t.Id == tilbudId);

        if (tilbud is null)
        {
            return null;
        }

        var linjer = await BuildLinjerAsync(tilbud);
        return linjer
            .Select(l => new LagretPdfLinje(l.Navn, l.Antall, l.LevertAv == LevertAv.F ? l.Utpris : null))
            .ToList();
    }

    private async Task<List<TilbudLinje>> BuildLinjerAsync(Tilbud tilbud)
    {
        var valgteByggetrinn = string.IsNullOrWhiteSpace(tilbud.ByggetrinnFilter)
            ? null
            : tilbud.ByggetrinnFilter.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var linjer = tilbud.Linjer.OrderBy(l => l.Rekkefolge).ToList();

        if (valgteByggetrinn is not null)
        {
            var antallPerComponent = await db.DorKomponenter
                .Where(dk => dk.Dor!.ProsjektId == tilbud.ProsjektId
                    && dk.Dor.Plantegning != null && valgteByggetrinn.Contains(dk.Dor.Plantegning.Byggetrinn))
                .GroupBy(dk => dk.ComponentId)
                .Select(g => new { ComponentId = g.Key, Antall = g.Sum(x => x.Antall) })
                .ToDictionaryAsync(x => x.ComponentId, x => x.Antall);

            linjer = linjer
                .Select(l => l.ComponentId is null ? l : new TilbudLinje
                {
                    Id = l.Id,
                    TilbudId = l.TilbudId,
                    ComponentId = l.ComponentId,
                    Component = l.Component,
                    Navn = l.Navn,
                    Innpris = l.Innpris,
                    EkstraRabattInnProsent = l.EkstraRabattInnProsent,
                    Utpris = l.Utpris,
                    Antall = antallPerComponent.GetValueOrDefault(l.ComponentId.Value),
                    Enhet = l.Enhet,
                    MontasjeMinutter = l.MontasjeMinutter,
                    LevertAv = l.LevertAv,
                    PrisType = l.PrisType,
                    Prosentsats = l.Prosentsats,
                    RabattProsent = l.RabattProsent,
                    Rekkefolge = l.Rekkefolge
                })
                .Where(l => l.ComponentId is null || l.Antall > 0)
                .ToList();
        }

        return linjer;
    }

    public async Task<byte[]?> GenerateAsync(int tilbudId)
    {
        var tilbud = await db.Tilbud
            .Include(t => t.Prosjekt)
            .Include(t => t.Linjer).ThenInclude(l => l.Component).ThenInclude(c => c!.Type)
            .FirstOrDefaultAsync(t => t.Id == tilbudId);

        if (tilbud is null)
        {
            return null;
        }

        var valgteByggetrinn = string.IsNullOrWhiteSpace(tilbud.ByggetrinnFilter)
            ? null
            : tilbud.ByggetrinnFilter.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var linjer = await BuildLinjerAsync(tilbud);

        var monteringMinutterPerComponent = await db.MonteringLinjer
            .Where(m => m.ProsjektId == tilbud.ProsjektId && m.ComponentId != null)
            .ToDictionaryAsync(m => m.ComponentId!.Value, m => m.Minutter);

        var visRabatt = linjer.Any(l => l.RabattProsent > 0);
        var utprisVarer = linjer.Where(l => l.LevertAv == LevertAv.F).Sum(l => l.Utpris * l.Antall);
        var minutter = linjer.Where(l => l.LevertAv == LevertAv.F)
            .Sum(l => ((l.ComponentId.HasValue && monteringMinutterPerComponent.TryGetValue(l.ComponentId.Value, out var m) ? m : null) ?? l.MontasjeMinutter ?? 0) * l.Antall);
        var arbeidstidTimerBase = tilbud.EstimertTimerOverride ?? (minutter / 60m);
        var arbeidstidTimer = arbeidstidTimerBase * (1 + (tilbud.RiggDriftProsent ?? 0) / 100m);
        var kalkulertMontasjekost = Math.Round(tilbud.Timepris * arbeidstidTimer, 2);
        var montasjekost = tilbud.Montasjekost ?? kalkulertMontasjekost;
        var totaltUtenMva = utprisVarer + montasjekost;

        var utprisPerComponent = linjer.Where(l => l.ComponentId.HasValue)
            .ToDictionary(l => l.ComponentId!.Value, l => l.Utpris);

        var dorer = new List<Dor>();
        if (tilbud.VisAlleDorerFraBeslagsliste)
        {
            dorer = await db.Dorer
                .Where(d => d.ProsjektId == tilbud.ProsjektId
                    && (valgteByggetrinn == null || (d.Plantegning != null && valgteByggetrinn.Contains(d.Plantegning.Byggetrinn))))
                .Include(d => d.Komponenter).ThenInclude(k => k.Component).ThenInclude(c => c!.Type)
                .Include(d => d.Funksjoner)
                .OrderBy(d => d.Dornummer)
                .ToListAsync();
        }

        var overskrift = tilbud.Prosjekt is not null ? $"{tilbud.Prosjekt.Navn} - {tilbud.Tittel}" : tilbud.Tittel;
        var dokumentTittel = tilbud.Type == Models.TilbudType.Endringsmelding ? "Endringsmelding" : "Tilbud";

        var document = Document.Create(doc =>
        {
            // Forside
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(140).AlignCenter().Text(dokumentTittel).FontSize(34).SemiBold();
                    col.Item().PaddingTop(8).AlignCenter().Text(overskrift).FontSize(17).FontColor(Colors.Grey.Darken2);
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("Prosjekt: ").Bold();
                            t.Span(tilbud.Prosjekt?.Navn ?? "");
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Kontaktperson: ").Bold();
                            t.Span(FirmaKontaktperson);
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Dato: ").Bold();
                            t.Span(tilbud.OpprettetDato.ToString("dd.MM.yyyy"));
                        });
                    });
                    row.RelativeItem(1);
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().AlignRight().Element(e => pdfLogo.Render(e, 16));
                        c.Item().AlignRight().Text(FirmaAdresse).FontSize(8);
                        c.Item().AlignRight().Text($"Telefon {FirmaTelefon}").FontSize(8);
                        c.Item().AlignRight().Text($"Epost {FirmaEpost}").FontSize(8);
                    });
                });
            });

            // Forbehold + produktsammendrag + totalsum
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(e => pdfLogo.Render(e, 15));
                        row.RelativeItem().AlignRight().Text(overskrift).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(tilbud.Forside))
                    {
                        ForsideRenderer.Render(col, tilbud.Forside);
                        col.Item().PageBreak();
                    }

                    var visSammendrag = tilbud.SummerAlleBeslag && linjer.Count > 0;
                    var visDorer = tilbud.VisAlleDorerFraBeslagsliste && dorer.Any(d => d.Komponenter.Any(k => k.Component is not null));

                    if (visSammendrag)
                    {
                        RenderProduktsammendrag(col, tilbud, linjer, visRabatt);
                    }

                    if (visDorer)
                    {
                        if (visSammendrag)
                        {
                            col.Item().PageBreak();
                        }

                        RenderDorer(col, tilbud, dorer, utprisPerComponent);
                    }

                    col.Item().PaddingTop(20).AlignRight().Width(260).Element(container => RenderTotalsBox(container, totaltUtenMva));
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

    private static void RenderProduktsammendrag(ColumnDescriptor col, Tilbud tilbud, List<TilbudLinje> linjer, bool visRabatt)
    {
        var visVarenummer = !tilbud.SkjulVarenummerISammendrag;
        var visLevering = linjer.Any(l => l.LevertAv != LevertAv.F);
        var visOverflate = linjer.Any(l => !string.IsNullOrWhiteSpace(l.Component?.Overflate));

        col.Item().Text("Produktsammendrag").FontSize(16).SemiBold();

        col.Item().PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                if (visVarenummer)
                {
                    columns.RelativeColumn(1.5f);
                }
                columns.RelativeColumn(3f);
                columns.RelativeColumn(2f);
                if (visOverflate)
                {
                    columns.RelativeColumn(1.3f);
                }
                columns.RelativeColumn(1f);
                if (visLevering)
                {
                    columns.RelativeColumn(1f);
                }
                if (tilbud.VisEnhetspris)
                {
                    if (visRabatt)
                    {
                        columns.RelativeColumn(1f);
                    }
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                }
            });

            table.Header(header =>
            {
                IContainer Hode() => header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).PaddingRight(6);

                if (visVarenummer)
                {
                    Hode().Text("Varenr").Bold();
                }
                Hode().Text("Varenavn").Bold();
                Hode().Text("Beslagstype").Bold();
                if (visOverflate)
                {
                    Hode().Text("Overflate").Bold();
                }
                Hode().Text("Ant.").Bold();
                if (visLevering)
                {
                    Hode().Text("Lev.").Bold();
                }
                if (tilbud.VisEnhetspris)
                {
                    if (visRabatt)
                    {
                        Hode().Text("Rabatt").Bold();
                    }
                    Hode().Text("Pris").Bold();
                    Hode().Text("Totalt").Bold();
                }
            });

            foreach (var l in linjer)
            {
                IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingRight(6);

                if (visVarenummer)
                {
                    Rad().Text(l.Component?.Produktkode ?? "");
                }
                Rad().Text(l.Navn);
                Rad().Text(l.Component?.Type?.Navn ?? "");
                if (visOverflate)
                {
                    Rad().Text(l.Component?.Overflate ?? "");
                }
                Rad().Text(l.Antall.ToString());
                if (visLevering)
                {
                    Rad().Text(l.LevertAv.Visningsnavn());
                }
                if (tilbud.VisEnhetspris)
                {
                    var visPrisPaLinje = l.LevertAv == LevertAv.F;
                    if (visRabatt)
                    {
                        Rad().Text(l.RabattProsent > 0 ? $"{l.RabattProsent.ToString("N0", Kultur)} %" : "-");
                    }
                    Rad().Text(visPrisPaLinje ? FormatKr(l.Utpris) : "–");
                    Rad().Text(visPrisPaLinje ? FormatKr(l.Utpris * l.Antall) : "–");
                }
            }
        });
    }

    private static void RenderDorer(ColumnDescriptor col, Tilbud tilbud, List<Dor> dorer, Dictionary<int, decimal> utprisPerComponent)
    {
        var relevanteDorer = dorer.Where(d => d.Komponenter.Any(k => k.Component is not null)).ToList();

        col.Item().Text("Dører").FontSize(16).SemiBold();

        decimal HentUtpris(DorKomponent k) => utprisPerComponent.GetValueOrDefault(k.ComponentId, k.Component!.PrisVeiledende ?? 0);

        for (var i = 0; i < relevanteDorer.Count; i++)
        {
            var dor = relevanteDorer[i];
            col.Item().PaddingTop(i == 0 ? 8 : 16).Column(inner => DorPdfSeksjoner.RenderDorSide(inner, dor, tilbud.VisPrisPerDor, HentUtpris));
        }
    }

    private void RenderTotalsBox(IContainer container, decimal totaltUtenMva)
    {
        var mva = Math.Round(totaltUtenMva * MvaSats, 2);
        var totaltInklMva = totaltUtenMva + mva;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
            });

            void Rad(string label, decimal verdi, bool bold, float strekTykkelse)
            {
                var labelText = table.Cell().BorderBottom(strekTykkelse).BorderColor(Colors.Grey.Darken2).PaddingVertical(4).Text(label);
                var verdiText = table.Cell().BorderBottom(strekTykkelse).BorderColor(Colors.Grey.Darken2).PaddingVertical(4).AlignRight().Text(FormatKr(verdi));
                if (bold)
                {
                    labelText.Bold();
                    verdiText.Bold();
                }
            }

            table.Cell().ColumnSpan(2).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
            Rad("Totalt uten MVA", totaltUtenMva, false, 2f);
            Rad("MVA", mva, false, 2f);
            Rad("Totalt inkl. MVA", totaltInklMva, true, 0f);
        });
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
