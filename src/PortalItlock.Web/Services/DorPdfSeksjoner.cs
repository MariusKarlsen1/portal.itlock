using System.Globalization;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

/// <summary>
/// Delt oppsett for å rendre én "dørside" (infoboks + dørfunksjoner + beslagsliste),
/// brukt både av den frittstående beslagslisten og av dør-seksjonen i tilbuds-PDFen.
/// Mal L14 (beslagsliste-pdf-forslag.html): fargestripe med dørnummer, dør til og
/// status, infofelter i fire kolonner, dørfunksjoner som merker, beslagstabell og
/// merknad - i organisasjonens farge (aksent/aksentLys), slik at malen passer for
/// alle bedriftene i portalen.
/// </summary>
public static class DorPdfSeksjoner
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");
    private static readonly Color Linje = Color.FromHex("#ECE4D6");

    public static void RenderDorSide(ColumnDescriptor col, Dor dor, bool visPris, Func<DorKomponent, decimal> hentUtpris,
        Color? aksent = null, Color? aksentLys = null)
    {
        var farge = aksent ?? PdfStil.Accent;
        var lys = aksentLys ?? PdfStil.Sand;
        var komponenter = dor.Komponenter.Where(k => k.Component is not null).OrderBy(k => k.Component!.Type?.Navn).ThenBy(k => k.Component!.Navn).ToList();

        // Fargestripe: dørnummer og dør til til venstre, status til høyre.
        col.Item().Background(lys).BorderBottom(1.5f).BorderColor(farge).CornerRadiusTopLeft(4).CornerRadiusTopRight(4).PaddingVertical(7).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().AlignMiddle().Text(t =>
            {
                t.Span(dor.Dornummer).FontSize(14).Bold().FontColor(farge);
                if (!string.IsNullOrWhiteSpace(dor.DorTil))
                {
                    t.Span("   " + dor.DorTil).FontSize(9).FontColor(RapportMal.Dempet);
                }
            });
            row.AutoItem().AlignMiddle().Background(Colors.White).CornerRadius(8).PaddingVertical(2).PaddingHorizontal(8)
                .Text(dor.Status.Visningsnavn()).FontSize(7.5f).Bold().FontColor(farge);
        });

        col.Item().Border(0.75f).BorderColor(PdfStil.SandBorder).CornerRadiusBottomLeft(4).CornerRadiusBottomRight(4).Padding(11).Column(inner =>
        {
            RenderInfoGrid(inner, dor, farge);

            if (dor.Funksjoner.Count > 0)
            {
                inner.Item().PaddingTop(9).Column(fc =>
                {
                    SeksjonTittel(fc, "Dørfunksjoner", farge, lys);
                    fc.Item().PaddingTop(4).Inlined(il =>
                    {
                        il.Spacing(4);
                        foreach (var f in dor.Funksjoner.OrderBy(f => f.Forkortelse ?? f.Navn))
                        {
                            il.Item().Background(lys).CornerRadius(8).PaddingVertical(2).PaddingHorizontal(7).Text(t =>
                            {
                                if (!string.IsNullOrWhiteSpace(f.Forkortelse))
                                {
                                    t.Span(f.Forkortelse + " ").FontSize(8).Bold().FontColor(farge);
                                }
                                t.Span(f.Navn).FontSize(8).SemiBold();
                            });
                        }
                    });
                });
            }

            RenderBeslagsliste(inner, komponenter, visPris, hentUtpris, farge, lys);

            inner.Item().PaddingTop(10).Background(lys).CornerRadius(3).PaddingVertical(7).PaddingHorizontal(9).Column(m =>
            {
                m.Item().Text("MERKNAD").FontSize(6.8f).Bold().FontColor(farge).LetterSpacing(0.08f);
                m.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(dor.Notater) ? "–" : dor.Notater).FontSize(9);
            });
        });
    }

    private static void SeksjonTittel(ColumnDescriptor col, string tittel, Color farge, Color lys)
    {
        col.Item().BorderBottom(0.75f).BorderColor(lys).PaddingBottom(2)
            .Text(tittel).FontSize(8.5f).Bold().FontColor(farge);
    }

    private static void RenderInfoGrid(ColumnDescriptor inner, Dor dor, Color farge)
    {
        inner.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });

            void Felt(string label, string? verdi)
            {
                table.Cell().PaddingVertical(3).PaddingRight(6).Column(fc =>
                {
                    fc.Item().Text(label.ToUpperInvariant()).FontSize(6.6f).Bold().FontColor(farge).LetterSpacing(0.05f);
                    fc.Item().PaddingTop(1).Text(string.IsNullOrWhiteSpace(verdi) ? "-" : verdi).FontSize(9.3f).SemiBold();
                });
            }

            Felt("Dør til", dor.DorTil);
            Felt("Rom nr.", dor.Romnr);
            Felt("Etasje", dor.Etasje);
            Felt("Sone", dor.Sone);
            Felt("Dørtype", dor.Dortype);
            Felt("B x H", dor.BxH ?? (dor.Bredde is null && dor.Hoyde is null ? null : $"{dor.Bredde} x {dor.Hoyde} mm"));
            Felt("Slagretning", dor.Slagretning);
            Felt("Fri bredde 0,86", dor.FriBredde086 is null ? null : (dor.FriBredde086.Value ? "Ja" : "Nei"));
            Felt("Status", dor.Status.Visningsnavn());
            Felt("Brann", dor.Brann);
            Felt("Lyd", dor.Lyd);
            Felt("Montert dato", dor.MontertDato?.ToString("dd.MM.yyyy"));
        });
    }

    private static void RenderBeslagsliste(ColumnDescriptor inner, List<DorKomponent> komponenter, bool visPris, Func<DorKomponent, decimal> hentUtpris, Color farge, Color lys)
    {
        inner.Item().PaddingTop(10).Column(bc =>
        {
            SeksjonTittel(bc, "Beslag", farge, lys);

            if (komponenter.Count == 0)
            {
                bc.Item().PaddingTop(4).Text("Ingen beslag registrert på denne døren.").FontSize(9).FontColor(Colors.Grey.Darken1);
                return;
            }

            var sumVarer = 0m;
            var visOverflate = komponenter.Any(k => !string.IsNullOrWhiteSpace(k.Component!.Overflate));

            bc.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.3f);
                    columns.RelativeColumn(3.4f);
                    if (visOverflate)
                    {
                        columns.RelativeColumn(1.4f);
                    }
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.5f);
                    if (visPris)
                    {
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                    }
                });

                table.Header(header =>
                {
                    IContainer Hode() => header.Cell().BorderBottom(1.2f).BorderColor(PdfStil.SandBorder).PaddingVertical(5).PaddingHorizontal(6);
                    Hode().Text("Beslagstype").FontSize(8).Bold().FontColor(RapportMal.Svak);
                    Hode().Text("Beskrivelse").FontSize(8).Bold().FontColor(RapportMal.Svak);
                    if (visOverflate)
                    {
                        Hode().Text("Overflate").FontSize(8).Bold().FontColor(RapportMal.Svak);
                    }
                    Hode().Text("Enhet").FontSize(8).Bold().FontColor(RapportMal.Svak);
                    Hode().AlignRight().Text("Antall").FontSize(8).Bold().FontColor(RapportMal.Svak);
                    Hode().Text("Levering").FontSize(8).Bold().FontColor(RapportMal.Svak);
                    if (visPris)
                    {
                        Hode().AlignRight().Text("Pris").FontSize(8).Bold().FontColor(RapportMal.Svak);
                        Hode().AlignRight().Text("Totalt").FontSize(8).Bold().FontColor(RapportMal.Svak);
                    }
                });

                foreach (var k in komponenter)
                {
                    var visPrisPaLinje = visPris && k.LevertAv == LevertAv.F;
                    IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingHorizontal(6);

                    Rad().Text(k.Component!.Type?.Navn ?? "").FontSize(8.5f);
                    Rad().Text(k.Component.Navn).FontSize(8.5f).SemiBold();
                    if (visOverflate)
                    {
                        Rad().Text(k.Component.Overflate ?? "").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    }
                    Rad().Text(k.Enhet ?? k.Component.Enhet ?? "Stk").FontSize(8.5f);
                    Rad().AlignRight().Text(k.Antall.ToString()).FontSize(8.5f).Bold();
                    Rad().Text(k.LevertAv.Visningsnavn()).FontSize(8.5f);
                    if (visPris)
                    {
                        var utpris = visPrisPaLinje ? hentUtpris(k) : 0m;
                        if (visPrisPaLinje)
                        {
                            sumVarer += utpris * k.Antall;
                        }
                        Rad().AlignRight().Text(visPrisPaLinje ? FormatKr(utpris) : "–").FontSize(8.5f);
                        Rad().AlignRight().Text(visPrisPaLinje ? FormatKr(utpris * k.Antall) : "–").FontSize(8.5f);
                    }
                }
            });

            if (visPris)
            {
                bc.Item().AlignRight().PaddingTop(4).Background(lys).PaddingVertical(4).PaddingHorizontal(8)
                    .Text($"Sum varer: {FormatKr(sumVarer)}").Bold().FontSize(9.5f).FontColor(farge);
            }
        });
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
