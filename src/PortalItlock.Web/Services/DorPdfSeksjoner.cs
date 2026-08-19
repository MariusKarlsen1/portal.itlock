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

        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Column(box =>
        {
            box.Item().Background(Colors.Grey.Lighten4).Padding(8).Text(dor.Dornummer).FontSize(14).Bold();
            box.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            box.Item().Padding(10).Column(inner =>
            {
                RenderInfoGrid(inner, dor);

                if (dor.Funksjoner.Count > 0)
                {
                    RenderDorfunksjoner(inner, dor);
                }

                RenderBeslagsliste(inner, dor, komponenter, visPris, hentUtpris);

                inner.Item().PaddingTop(10).Text("Merknad").FontSize(9).Bold();
                inner.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(dor.Notater) ? "–" : dor.Notater).FontSize(9);
            });
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
                table.Cell().PaddingRight(10).PaddingBottom(6).Column(fc =>
                {
                    fc.Item().Text(label).FontSize(7).Bold().FontColor(Colors.Grey.Darken2);
                    fc.Item().PaddingTop(1).BorderBottom(0.75f).BorderColor(Colors.Grey.Lighten2).PaddingBottom(1)
                        .Text(string.IsNullOrWhiteSpace(verdi) ? " " : verdi).FontSize(9);
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
        inner.Item().PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(60);
                c.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Dørfunk.").Bold();
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Beskrivelse").Bold();
            });

            foreach (var f in dor.Funksjoner.OrderBy(f => f.Forkortelse ?? f.Navn))
            {
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).Text(f.Forkortelse ?? "");
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).Text(f.Navn);
            }
        });
    }

    private static void RenderBeslagsliste(ColumnDescriptor inner, Dor dor, List<DorKomponent> komponenter, bool visPris, Func<DorKomponent, decimal> hentUtpris)
    {
        inner.Item().PaddingTop(20).Text("Beslagsliste").FontSize(12).Bold();

        if (komponenter.Count == 0)
        {
            inner.Item().PaddingTop(3).Text("Ingen beslag registrert på denne døren.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var sumVarer = 0m;
        var visOverflate = komponenter.Any(k => !string.IsNullOrWhiteSpace(k.Component!.Overflate));

        inner.Item().PaddingTop(3).Table(table =>
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
                IContainer Hode() => header.Cell().BorderBottom(1.25f).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).PaddingRight(6);

                Hode().Text("Beslagstype").Bold();
                Hode().Text("Beskrivelse").Bold();
                if (visOverflate)
                {
                    Hode().Text("Overflate").Bold();
                }
                Hode().Text("Enhet").Bold();
                Hode().Text("Antall").Bold();
                Hode().Text("Levering").Bold();
                if (visPris)
                {
                    Hode().Text("Pris").Bold();
                    Hode().Text("Totalt").Bold();
                }
            });

            foreach (var k in komponenter)
            {
                var visPrisPaLinje = visPris && k.LevertAv == LevertAv.F;

                IContainer Rad() => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingRight(6);

                Rad().Text(k.Component!.Type?.Navn ?? "");
                Rad().Text(k.Component.Navn);
                if (visOverflate)
                {
                    Rad().Text(k.Component.Overflate ?? "");
                }
                Rad().Text(k.Enhet ?? k.Component.Enhet ?? "Stk");
                Rad().Text(k.Antall.ToString());
                Rad().Text(k.LevertAv.Visningsnavn());

                if (visPris)
                {
                    var utpris = visPrisPaLinje ? hentUtpris(k) : 0m;
                    if (visPrisPaLinje)
                    {
                        sumVarer += utpris * k.Antall;
                    }

                    Rad().Text(visPrisPaLinje ? FormatKr(utpris) : "–");
                    Rad().Text(visPrisPaLinje ? FormatKr(utpris * k.Antall) : "–");
                }
            }
        });

        if (visPris)
        {
            inner.Item().AlignRight().PaddingTop(4).Text($"Sum varer: {FormatKr(sumVarer)}").Bold().FontSize(9);
        }
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
