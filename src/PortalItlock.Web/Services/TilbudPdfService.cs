using System.Globalization;
using System.Text.RegularExpressions;
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

        var linjer = tilbud.Linjer.OrderBy(l => l.Rekkefolge).ToList();
        var visRabatt = linjer.Any(l => l.RabattProsent > 0);
        var utprisVarer = linjer.Where(l => l.LevertAv == LevertAv.F).Sum(l => l.Utpris * l.Antall);
        var minutter = linjer.Where(l => l.LevertAv == LevertAv.F).Sum(l => (l.MontasjeMinutter ?? 0) * l.Antall);
        var arbeidstidTimer = tilbud.EstimertTimerOverride ?? (minutter / 60m);
        var kalkulertMontasjekost = Math.Round(tilbud.Timepris * arbeidstidTimer, 2);
        var montasjekost = tilbud.Montasjekost ?? kalkulertMontasjekost;
        var totaltUtenMva = utprisVarer + montasjekost;

        var utprisPerComponent = linjer.Where(l => l.ComponentId.HasValue)
            .ToDictionary(l => l.ComponentId!.Value, l => l.Utpris);

        var dorer = new List<Dor>();
        if (tilbud.VisAlleDorerFraBeslagsliste)
        {
            dorer = await db.Dorer
                .Where(d => d.ProsjektId == tilbud.ProsjektId)
                .Include(d => d.Komponenter).ThenInclude(k => k.Component).ThenInclude(c => c!.Type)
                .Include(d => d.Funksjoner)
                .OrderBy(d => d.Dornummer)
                .ToListAsync();
        }

        var overskrift = tilbud.Prosjekt is not null ? $"{tilbud.Prosjekt.Navn} - {tilbud.Tittel}" : tilbud.Tittel;

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
                    col.Item().PaddingTop(140).AlignCenter().Text("Tilbud").FontSize(34).SemiBold();
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
                        RenderForside(col, tilbud.Forside);
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
                if (visVarenummer)
                {
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Varenr").Bold();
                }
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Varenavn").Bold();
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Beslagstype").Bold();
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Ant.").Bold();
                if (visLevering)
                {
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Lev.").Bold();
                }
                if (tilbud.VisEnhetspris)
                {
                    if (visRabatt)
                    {
                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Rabatt").Bold();
                    }
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Pris").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Totalt").Bold();
                }
            });

            foreach (var l in linjer)
            {
                IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3);

                if (visVarenummer)
                {
                    Rad().Text(l.Component?.Produktkode ?? "");
                }
                Rad().Text(l.Navn);
                Rad().Text(l.Component?.Type?.Navn ?? "");
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

    private static readonly Regex InlineMarkupRegex = new(
        @"\*\*(?<bold>.+?)\*\*|\{\{size=(?<sizeval>\d+(?:\.\d+)?)\}\}(?<sizetext>.+?)\{\{/size\}\}|\{\{font=(?<fontval>[^}]+)\}\}(?<fonttext>.+?)\{\{/font\}\}",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static void RenderInlineText(TextDescriptor t, string text)
    {
        var lastIndex = 0;
        foreach (Match m in InlineMarkupRegex.Matches(text))
        {
            if (m.Index > lastIndex)
            {
                t.Span(text[lastIndex..m.Index]);
            }

            if (m.Groups["bold"].Success)
            {
                t.Span(m.Groups["bold"].Value).Bold();
            }
            else if (m.Groups["sizeval"].Success)
            {
                t.Span(m.Groups["sizetext"].Value).FontSize(float.Parse(m.Groups["sizeval"].Value, CultureInfo.InvariantCulture));
            }
            else if (m.Groups["fontval"].Success)
            {
                t.Span(m.Groups["fonttext"].Value).FontFamily([m.Groups["fontval"].Value.Trim()]);
            }

            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < text.Length)
        {
            t.Span(text[lastIndex..]);
        }
    }

    private static void RenderForside(ColumnDescriptor col, string tekst)
    {
        var paragraph = new List<string>();

        void FlushParagraph()
        {
            if (paragraph.Count == 0)
            {
                return;
            }

            var joined = string.Join("\n", paragraph);
            col.Item().PaddingBottom(4).Text(t =>
            {
                t.DefaultTextStyle(x => x.LineHeight(1.35f));
                RenderInlineText(t, joined);
            });
            paragraph.Clear();
        }

        foreach (var raw in tekst.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("## "))
            {
                FlushParagraph();
                col.Item().PaddingTop(4).Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(11).Bold());
                    RenderInlineText(t, line[3..].Trim());
                });
            }
            else if (line.StartsWith("# "))
            {
                FlushParagraph();
                col.Item().PaddingTop(8).Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(19).SemiBold());
                    RenderInlineText(t, line[2..].Trim());
                });
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
            }
            else
            {
                paragraph.Add(line.Trim());
            }
        }

        FlushParagraph();
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

            void Rad(string label, decimal verdi, bool bold)
            {
                var labelText = table.Cell().PaddingVertical(3).Text(label);
                var verdiText = table.Cell().PaddingVertical(3).AlignRight().Text(FormatKr(verdi));
                if (bold)
                {
                    labelText.Bold();
                    verdiText.Bold();
                }
            }

            table.Cell().ColumnSpan(2).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
            Rad("Totalt uten MVA", totaltUtenMva, false);
            Rad("MVA", mva, false);
            table.Cell().ColumnSpan(2).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
            Rad("Totalt inkl. MVA", totaltInklMva, true);
        });
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
