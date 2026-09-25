using System.Globalization;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

/// <summary>
/// Delt oppsett for å rendre én "dørside" (infoboks + dørfunksjoner + beslagsliste),
/// brukt både av den frittstående beslagslisten og av dør-seksjonen i tilbuds-PDFen.
/// Alt for én dør sitter i én sammenhengende ramme (kort), slik Probe sin rapport viser det.
/// </summary>
public static class DorPdfSeksjoner
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public static void RenderDorSide(ColumnDescriptor col, Dor dor, bool visPris, Func<DorKomponent, decimal> hentUtpris)
    {
        var komponenter = dor.Komponenter.Where(k => k.Component is not null).OrderBy(k => k.Component!.Type?.Navn).ThenBy(k => k.Component!.Navn).ToList();

        col.Item().Background(PdfStil.Accent).Padding(7).Row(row =>
        {
            row.RelativeItem().Text(dor.Dornummer).FontSize(13).Bold().FontColor(Colors.White);
            if (!string.IsNullOrWhiteSpace(dor.DorTil))
            {
                row.AutoItem().Background(Color.FromHex("#A5805F")).PaddingVertical(2).PaddingHorizontal(8).Text(dor.DorTil).FontSize(8).Bold().FontColor(Colors.White);
            }
        });

        col.Item().Border(1).BorderColor(PdfStil.SandBorder).Padding(10).Column(inner =>
        {
            RenderInfoGrid(inner, dor);

            if (dor.Funksjoner.Count > 0)
            {
                RenderDorfunksjoner(inner, dor);
            }

            RenderBeslagsliste(inner, dor, komponenter, visPris, hentUtpris);

            inner.Item().PaddingTop(8);
            PdfStil.NotatBoks(inner, "Merknad", string.IsNullOrWhiteSpace(dor.Notater) ? "–" : dor.Notater);
        });
    }

    private static void RenderInfoGrid(ColumnDescriptor inner, Dor dor)
    {
        inner.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });

            void Felt(string label, string? verdi)
            {
                table.Cell().Padding(3).Column(fc =>
                {
                    fc.Item().Text(label.ToUpperInvariant()).FontSize(6.8f).Bold().FontColor(PdfStil.Accent).LetterSpacing(0.03f);
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

    private static void RenderDorfunksjoner(ColumnDescriptor inner, Dor dor)
    {
        inner.Item().PaddingTop(8).Column(dc =>
        {
            PdfStil.SeksjonTittel(dc, "Dørfunksjoner");
            dc.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(60);
                    c.RelativeColumn();
                });

                foreach (var f in dor.Funksjoner.OrderBy(f => f.Forkortelse ?? f.Navn))
                {
                    table.Cell().PaddingVertical(2).Text(f.Forkortelse ?? "").FontSize(8.5f).Bold().FontColor(PdfStil.Accent);
                    table.Cell().PaddingVertical(2).Text(f.Navn).FontSize(8.5f);
                }
            });
        });
    }

    private static void RenderBeslagsliste(ColumnDescriptor inner, Dor dor, List<DorKomponent> komponenter, bool visPris, Func<DorKomponent, decimal> hentUtpris)
    {
        inner.Item().PaddingTop(10).Column(bc =>
        {
            PdfStil.SeksjonTittel(bc, "Beslagsliste");

            if (komponenter.Count == 0)
            {
                bc.Item().PaddingTop(3).Text("Ingen beslag registrert på denne døren.").FontSize(9).FontColor(Colors.Grey.Darken1);
                return;
            }

            var sumVarer = 0m;
            var visOverflate = komponenter.Any(k => !string.IsNullOrWhiteSpace(k.Component!.Overflate));

            bc.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(3.2f);
                    if (visOverflate)
                    {
                        columns.RelativeColumn(1.4f);
                    }
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.4f);
                    if (visPris)
                    {
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                    }
                });

                table.Header(header =>
                {
                    IContainer Hode() => PdfStil.TabellHode(header.Cell());

                    Hode().Text("Beslagstype").FontSize(8).Bold().FontColor(Colors.White);
                    Hode().Text("Beskrivelse").FontSize(8).Bold().FontColor(Colors.White);
                    if (visOverflate)
                    {
                        Hode().Text("Overflate").FontSize(8).Bold().FontColor(Colors.White);
                    }
                    Hode().Text("Enhet").FontSize(8).Bold().FontColor(Colors.White);
                    Hode().Text("Antall").FontSize(8).Bold().FontColor(Colors.White);
                    Hode().Text("Levering").FontSize(8).Bold().FontColor(Colors.White);
                    if (visPris)
                    {
                        Hode().Text("Pris").FontSize(8).Bold().FontColor(Colors.White);
                        Hode().Text("Totalt").FontSize(8).Bold().FontColor(Colors.White);
                    }
                });

                var i = 0;
                foreach (var k in komponenter)
                {
                    var visPrisPaLinje = visPris && k.LevertAv == LevertAv.F;
                    var alt = i++ % 2 == 1;

                    IContainer Rad() => PdfStil.TabellRad(table.Cell(), alt);

                    Rad().Text(k.Component!.Type?.Navn ?? "").FontSize(8.5f);
                    Rad().Text(k.Component.Navn).FontSize(8.5f);
                    if (visOverflate)
                    {
                        Rad().Text(k.Component.Overflate ?? "").FontSize(8.5f);
                    }
                    Rad().Text(k.Enhet ?? k.Component.Enhet ?? "Stk").FontSize(8.5f);
                    Rad().Text(k.Antall.ToString()).FontSize(8.5f);
                    Rad().Text(k.LevertAv.Visningsnavn()).FontSize(8.5f);

                    if (visPris)
                    {
                        var utpris = visPrisPaLinje ? hentUtpris(k) : 0m;
                        if (visPrisPaLinje)
                        {
                            sumVarer += utpris * k.Antall;
                        }

                        Rad().Text(visPrisPaLinje ? FormatKr(utpris) : "–").FontSize(8.5f);
                        Rad().Text(visPrisPaLinje ? FormatKr(utpris * k.Antall) : "–").FontSize(8.5f);
                    }
                }
            });

            if (visPris)
            {
                bc.Item().AlignRight().PaddingTop(4).Background(PdfStil.Sand).PaddingVertical(4).PaddingHorizontal(8)
                    .Text($"Sum varer: {FormatKr(sumVarer)}").Bold().FontSize(9.5f).FontColor(PdfStil.Accent);
            }
        });
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
