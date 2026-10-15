using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class TilbudPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
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
            .Include(t => t.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(t => t.Prosjekt).ThenInclude(p => p!.Prosjektleder)
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

        // Mal M7 (tilbudpdf-forslag.html): forsiden med fargekolonne til venstre
        // (logo, kontaktperson, firma) og tittel/kunde/datoer/pris til høyre,
        // deretter forbehold, produktsammendrag med montering som egen linje og
        // totalsum. Avsenderen er organisasjonen som er logget inn.
        var org = RapportProfil.Fra(tenantContext.Current);
        var dokumentTittel = tilbud.Type == Models.TilbudType.Endringsmelding ? "Endringsmelding" : "Tilbud";
        var tilbudsnummer = $"T-{tilbud.OpprettetDato.Year}-{tilbud.Id:000}";
        var prosjektleder = tilbud.Prosjekt?.Prosjektleder;
        var kontaktNavn = prosjektleder?.Navn ?? (org.ErStandard ? FirmaInfo.Kontaktperson : null);
        var kontaktEpost = prosjektleder?.Epost ?? org.Epost;
        var kunde = tilbud.Prosjekt?.Kunde;
        var kundeAdresse = kunde is null ? null : string.Join(", ", new[] { kunde.Adresse, string.Join(" ", new[] { kunde.Postnr, kunde.Sted }.Where(s => !string.IsNullOrWhiteSpace(s))) }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var bunnLinje = string.Join(" · ", new[] { org.Navn, org.Adresse, org.OrgNr is null ? null : $"Org.nr {org.OrgNr}" }.Where(s => !string.IsNullOrWhiteSpace(s)));

        static void Etikett(ColumnDescriptor c, string tekst, Color farge) =>
            c.Item().Text(tekst.ToUpperInvariant()).FontSize(7f).Bold().FontColor(farge).LetterSpacing(0.08f);

        var document = Document.Create(doc =>
        {
            // Forside
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Ink));

                page.Content().Row(row =>
                {
                    // Fargekolonnen til venstre
                    row.ConstantItem(160).Background(org.AksentLys).PaddingVertical(42).PaddingHorizontal(20).Column(kol =>
                    {
                        kol.Item().Element(e => org.TegnLogo(e, pdfLogo, 26));
                        kol.Item().ExtendVertical().AlignBottom().Column(bunn =>
                        {
                        if (!string.IsNullOrWhiteSpace(kontaktNavn))
                        {
                            Etikett(bunn, "Kontaktperson", org.Aksent);
                            bunn.Item().PaddingTop(2).Text(kontaktNavn).FontSize(9.5f).SemiBold();
                            if (!string.IsNullOrWhiteSpace(org.Telefon))
                            {
                                bunn.Item().Text(org.Telefon).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                            }
                            if (!string.IsNullOrWhiteSpace(kontaktEpost))
                            {
                                bunn.Item().Text(kontaktEpost).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                            }
                            bunn.Item().Height(12);
                        }
                        Etikett(bunn, "Firma", org.Aksent);
                        bunn.Item().PaddingTop(2).Text(org.Navn).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                        if (!string.IsNullOrWhiteSpace(org.Adresse))
                        {
                            bunn.Item().Text(org.Adresse).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                        }
                        if (!string.IsNullOrWhiteSpace(org.OrgNr))
                        {
                            bunn.Item().Text($"Org.nr {org.OrgNr}").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                        }
                        });
                    });

                    // Tittel, kunde, datoer og pris
                    row.RelativeItem().PaddingVertical(42).PaddingHorizontal(40).Column(kol =>
                    {
                        kol.Item().Height(150);
                        Etikett(kol, $"{dokumentTittel} {tilbudsnummer}", org.Aksent);
                        kol.Item().PaddingTop(6).Text(tilbud.Tittel).FontSize(28).Bold().LineHeight(1.15f);
                        if (tilbud.Prosjekt is not null)
                        {
                            kol.Item().PaddingTop(4).Text(tilbud.Prosjekt.Navn).FontSize(12).FontColor(Colors.Grey.Darken2);
                        }

                        if (kunde is not null || !string.IsNullOrWhiteSpace(tilbud.KontaktpersonNavn))
                        {
                            kol.Item().PaddingTop(56).Column(til =>
                            {
                                Etikett(til, "Til", org.Aksent);
                                if (kunde is not null)
                                {
                                    til.Item().PaddingTop(2).Text(kunde.Navn).FontSize(12).SemiBold();
                                    if (!string.IsNullOrWhiteSpace(kundeAdresse))
                                    {
                                        til.Item().Text(kundeAdresse).FontSize(9).FontColor(Colors.Grey.Darken2);
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(tilbud.KontaktpersonNavn))
                                {
                                    var kontakt = string.Join(" · ", new[] { $"Att: {tilbud.KontaktpersonNavn}", tilbud.KontaktpersonTelefon }.Where(s => !string.IsNullOrWhiteSpace(s)));
                                    til.Item().Text(kontakt).FontSize(9).FontColor(Colors.Grey.Darken2);
                                }
                            });
                        }

                        kol.Item().PaddingTop(22).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            void Felt(string etikett, string verdi) => t.Cell().PaddingBottom(10).Column(c =>
                            {
                                Etikett(c, etikett, org.Aksent);
                                c.Item().PaddingTop(1).Text(verdi).FontSize(10).SemiBold();
                            });

                            Felt("Dato", tilbud.OpprettetDato.ToString("dd.MM.yyyy"));
                            Felt("Gyldig til", tilbud.GyldigTil?.ToString("dd.MM.yyyy") ?? "-");
                            Felt("Levering", string.IsNullOrWhiteSpace(tilbud.Leveringstid) ? "-" : tilbud.Leveringstid);
                        });

                        kol.Item().ExtendVertical().AlignBottom().BorderTop(1.5f).BorderColor(org.Aksent).PaddingTop(10).Row(sum =>
                        {
                            sum.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Totalt eks. mva").FontSize(9).FontColor(Colors.Grey.Darken2);
                                c.Item().Text($"{FormatKrHel(totaltUtenMva * (1 + MvaSats))} inkl. mva").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            });
                            sum.AutoItem().AlignBottom().Text(FormatKrHel(totaltUtenMva)).FontSize(24).Bold();
                        });
                    });
                });
            });

            // Forbehold + produktsammendrag + totalsum
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Ink));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().AlignBottom().Element(e => org.TegnLogo(e, pdfLogo, 20));
                        row.RelativeItem().AlignRight().AlignBottom().Column(c =>
                        {
                            c.Item().AlignRight().Text(tilbud.Tittel).FontSize(9).SemiBold();
                            c.Item().AlignRight().Text($"{dokumentTittel} {tilbudsnummer} · {tilbud.OpprettetDato:dd.MM.yyyy}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(org.Aksent);
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(tilbud.Forside))
                    {
                        ForsideRenderer.Render(col, tilbud.Forside);
                        col.Item().Height(14);
                    }

                    var visSammendrag = tilbud.SummerAlleBeslag && linjer.Count > 0;
                    var visDorer = tilbud.VisAlleDorerFraBeslagsliste && dorer.Any(d => d.Komponenter.Any(k => k.Component is not null));

                    if (visSammendrag)
                    {
                        RenderProduktsammendrag(col, tilbud, linjer, visRabatt, arbeidstidTimer, montasjekost, org.Aksent);
                    }

                    if (visDorer)
                    {
                        if (visSammendrag)
                        {
                            col.Item().PageBreak();
                        }

                        RenderDorer(col, tilbud, dorer, utprisPerComponent);
                    }

                    col.Item().PaddingTop(20).AlignRight().Width(260).Element(container => RenderTotalsBox(container, utprisVarer, montasjekost, totaltUtenMva, org.Aksent));
                });

                page.Footer().BorderTop(0.5f).BorderColor(SandBorder).PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(bunnLinje).FontSize(7).FontColor(Colors.Grey.Darken1);
                    row.AutoItem().Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                        x.Span($"{dokumentTittel} {tilbudsnummer} · side ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void RenderProduktsammendrag(ColumnDescriptor col, Tilbud tilbud, List<TilbudLinje> linjer, bool visRabatt, decimal monteringTimer, decimal montasjekost, Color aksent)
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
                IContainer Hode() => header.Cell().Background(aksent).PaddingVertical(5).PaddingHorizontal(6);

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
                    Hode().AlignRight().Text("Pris").FontSize(8).Bold().FontColor(Colors.White);
                    Hode().AlignRight().Text("Totalt").FontSize(8).Bold().FontColor(Colors.White);
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
                                inner.Item().Text(l.Navn).Bold().FontColor(aksent);
                            }
                            if (!string.IsNullOrWhiteSpace(l.Beskrivelse))
                            {
                                inner.Item().PaddingTop(2).Text(l.Beskrivelse).FontSize(9);
                            }
                        });
                    continue;
                }

                IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(SandBorder).PaddingVertical(4).PaddingHorizontal(6);



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
                    Rad().AlignRight().Text(visPrisPaLinje ? FormatKr(l.Utpris) : "–");
                    Rad().AlignRight().Text(visPrisPaLinje ? FormatKr(l.Utpris * l.Antall) : "–");
                }
            }

            // Montering som egen linje nederst i sammendraget.
            if (montasjekost > 0)
            {
                var tekst = monteringTimer > 0
                    ? $"Montering og igangsetting ({monteringTimer.ToString("0.#", Kultur)} t)"
                    : "Montering og igangsetting";
                IContainer Mont(uint span = 1) => table.Cell().ColumnSpan(span).Background(Color.FromHex("#FBF8F3"))
                    .BorderBottom(0.5f).BorderColor(SandBorder).PaddingVertical(5).PaddingHorizontal(6);

                if (tilbud.VisEnhetspris)
                {
                    Mont((uint)(totaltAntallKolonner - 1)).Text(tekst).Italic();
                    Mont().AlignRight().Text(FormatKr(montasjekost)).Italic();
                }
                else
                {
                    Mont((uint)totaltAntallKolonner).Row(r =>
                    {
                        r.RelativeItem().Text(tekst).Italic();
                        r.AutoItem().Text(FormatKr(montasjekost)).Italic();
                    });
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

    private static void RenderTotalsBox(IContainer container, decimal utprisVarer, decimal montasjekost, decimal totaltUtenMva, Color aksent)
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

            if (montasjekost > 0)
            {
                Rad("Varer", utprisVarer);
                Rad("Montering", montasjekost);
            }
            Rad("Totalt uten MVA", totaltUtenMva);
            Rad("MVA (25 %)", mva);

            col.Item().Background(aksent).Padding(9).Row(row =>
            {
                row.RelativeItem().Text("Totalt inkl. MVA").FontSize(11).Bold().FontColor(Colors.White);
                row.AutoItem().Text(FormatKr(totaltInklMva)).FontSize(12).Bold().FontColor(Colors.White);
            });
        });
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";

    private static string FormatKrHel(decimal value) => value.ToString("N0", Kultur) + " kr";
}
