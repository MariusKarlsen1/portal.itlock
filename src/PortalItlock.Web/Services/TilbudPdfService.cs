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
    private static readonly Color Sand = Color.FromHex("#F2EBE1");
    private static readonly Color SandBorder = Color.FromHex("#E4D9C8");
    private static readonly Color Accent = Color.FromHex("#835E41");
    private static readonly Color Ink = Color.FromHex("#292927");

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
                    PrisVeiledende = l.PrisVeiledende,
                    EkstraRabattInnProsent = l.EkstraRabattInnProsent,
                    Utpris = l.Utpris,
                    Antall = antallPerComponent.GetValueOrDefault(l.ComponentId.Value),
                    Enhet = l.Enhet,
                    MontasjeMinutter = l.MontasjeMinutter,
                    LevertAv = l.LevertAv,
                    PrisType = l.PrisType,
                    Prosentsats = l.Prosentsats,
                    RabattProsent = l.RabattProsent,
                    Rekkefolge = l.Rekkefolge,
                    ErGruppering = l.ErGruppering,
                    Beskrivelse = l.Beskrivelse
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
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Ink));

                page.Content().Column(col =>
                {
                    col.Item().Height(6).Background(Accent);

                    col.Item().Padding(2, Unit.Centimetre).Column(inner =>
                    {
                        inner.Item().Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(150).AlignRight().Element(e => pdfLogo.Render(e, 30));
                        });

                        inner.Item().PaddingTop(120).Text(dokumentTittel.ToUpperInvariant()).FontSize(11).Bold().FontColor(Accent).LetterSpacing(0.1f);
                        inner.Item().PaddingTop(6).Text(overskrift).FontSize(30).Bold();
                        if (tilbud.Prosjekt is not null)
                        {
                            inner.Item().PaddingTop(4).Text(tilbud.Prosjekt.Navn).FontSize(13).FontColor(Colors.Grey.Darken2);
                        }

                        inner.Item().PaddingTop(190).BorderTop(1).BorderColor(SandBorder).PaddingTop(14).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("KONTAKTPERSON").FontSize(7.5f).Bold().FontColor(Accent).LetterSpacing(0.05f);
                                c.Item().PaddingTop(2).Text(FirmaInfo.Kontaktperson).FontSize(9.5f).SemiBold();
                                c.Item().Text($"{FirmaInfo.Telefon} · {FirmaInfo.Epost}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("DATO").FontSize(7.5f).Bold().FontColor(Accent).LetterSpacing(0.05f);
                                c.Item().PaddingTop(2).Text(tilbud.OpprettetDato.ToString("dd.MM.yyyy")).FontSize(9.5f).SemiBold();
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text(FirmaInfo.Navn).FontSize(9.5f).SemiBold();
                                c.Item().AlignRight().Text(FirmaInfo.AdresseFull).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                c.Item().AlignRight().Text($"Tlf {FirmaInfo.Telefon} · {FirmaInfo.Epost}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            });
                        });
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
                        row.RelativeItem().Element(e => pdfLogo.Render(e, 20));
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text(overskrift).FontSize(9).SemiBold();
                            c.Item().AlignRight().Text(dokumentTittel).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Accent);
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
        var totaltAntallKolonner = (visVarenummer ? 1 : 0) + 1 + 1 + (visOverflate ? 1 : 0) + 1 + (visLevering ? 1 : 0)
            + (tilbud.VisEnhetspris ? (visRabatt ? 1 : 0) + 2 : 0);

        col.Item().Text("Produktsammendrag").FontSize(16).Bold().FontColor(Ink);

        col.Item().PaddingTop(8).Table(table =>
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
                IContainer Hode() => header.Cell().Background(Accent).PaddingVertical(5).PaddingHorizontal(6);

                if (visVarenummer)
                {
                    Hode().Text("Varenr").FontSize(8).Bold().FontColor(Colors.White);
                }
                Hode().Text("Varenavn").FontSize(8).Bold().FontColor(Colors.White);
                Hode().Text("Beslagstype").FontSize(8).Bold().FontColor(Colors.White);
                if (visOverflate)
                {
                    Hode().Text("Overflate").FontSize(8).Bold().FontColor(Colors.White);
                }
                Hode().Text("Ant.").FontSize(8).Bold().FontColor(Colors.White);
                if (visLevering)
                {
                    Hode().Text("Lev.").FontSize(8).Bold().FontColor(Colors.White);
                }
                if (tilbud.VisEnhetspris)
                {
                    if (visRabatt)
                    {
                        Hode().Text("Rabatt").FontSize(8).Bold().FontColor(Colors.White);
                    }
                    Hode().Text("Pris").FontSize(8).Bold().FontColor(Colors.White);
                    Hode().Text("Totalt").FontSize(8).Bold().FontColor(Colors.White);
                }
            });

            var radIndeks = 0;
            foreach (var l in linjer)
            {
                if (l.ErGruppering)
                {
                    table.Cell().ColumnSpan((uint)totaltAntallKolonner)
                        .Background(Sand).BorderBottom(0.5f).BorderColor(SandBorder).PaddingVertical(5).PaddingHorizontal(6)
                        .Column(inner =>
                        {
                            if (!string.IsNullOrWhiteSpace(l.Navn))
                            {
                                inner.Item().Text(l.Navn).Bold().FontColor(Accent);
                            }
                            if (!string.IsNullOrWhiteSpace(l.Beskrivelse))
                            {
                                inner.Item().PaddingTop(2).Text(l.Beskrivelse).FontSize(9);
                            }
                        });
                    continue;
                }

                radIndeks++;
                var radBakgrunn = radIndeks % 2 == 0 ? Sand : Colors.White;
                IContainer Rad() => table.Cell().Background(radBakgrunn).BorderBottom(0.5f).BorderColor(SandBorder).PaddingVertical(4).PaddingHorizontal(6);

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

        container.Border(1).BorderColor(SandBorder).Column(col =>
        {
            void Rad(string label, decimal verdi)
            {
                col.Item().BorderBottom(0.5f).BorderColor(SandBorder).Padding(8).Row(row =>
                {
                    row.RelativeItem().Text(label).FontSize(9.5f);
                    row.AutoItem().Text(FormatKr(verdi)).FontSize(9.5f);
                });
            }

            Rad("Totalt uten MVA", totaltUtenMva);
            Rad("MVA (25%)", mva);

            col.Item().Background(Accent).Padding(9).Row(row =>
            {
                row.RelativeItem().Text("Totalt inkl. MVA").FontSize(11).Bold().FontColor(Colors.White);
                row.AutoItem().Text(FormatKr(totaltInklMva)).FontSize(12).Bold().FontColor(Colors.White);
            });
        });
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
