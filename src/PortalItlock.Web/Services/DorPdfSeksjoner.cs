using System.Globalization;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

/// <summary>
/// Delt oppsett for å rendre én "dørside" (infoboks + dørfunksjoner + beslagsliste),
/// brukt både av den frittstående beslagslisten og av dør-seksjonen i tilbuds-PDFen.
/// </summary>
public static class DorPdfSeksjoner
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public static void RenderDorSide(ColumnDescriptor col, Dor dor, bool visPris, Func<DorKomponent, decimal> hentUtpris)
    {
        var komponenter = dor.Komponenter.Where(k => k.Component is not null).OrderBy(k => k.Component!.Type?.Navn).ThenBy(k => k.Component!.Navn).ToList();

        col.Item().Text(dor.Dornummer).FontSize(18).SemiBold();

        col.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Table(table =>
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
                    fc.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken1);
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

        if (dor.Funksjoner.Count > 0)
        {
            col.Item().PaddingTop(10).Text("Dørfunksjoner").FontSize(11).SemiBold();
            col.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(50);
                    c.RelativeColumn();
                });

                foreach (var f in dor.Funksjoner.OrderBy(f => f.Forkortelse ?? f.Navn))
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).Text(f.Forkortelse ?? "");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).Text(f.Navn);
                }
            });
        }

        col.Item().PaddingTop(10).Text("Beslagsliste").FontSize(11).SemiBold();

        if (komponenter.Count == 0)
        {
            col.Item().PaddingTop(3).Text("Ingen beslag registrert på denne døren.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var sumVarer = 0m;

        col.Item().PaddingTop(3).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(3.5f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(0.7f);
                if (visPris)
                {
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                }
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Beslagstype").Bold();
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Beskrivelse").Bold();
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Enhet").Bold();
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Antall").Bold();
                header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Lev.").Bold();
                if (visPris)
                {
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Pris").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingBottom(3).Text("Totalt").Bold();
                }
            });

            foreach (var k in komponenter)
            {
                var visPrisPaLinje = visPris && k.LevertAv == LevertAv.F;

                table.Cell().Text(k.Component!.Type?.Navn ?? "");
                table.Cell().Text(k.Component.Navn);
                table.Cell().Text(k.Enhet ?? k.Component.Enhet ?? "Stk");
                table.Cell().Text(k.Antall.ToString());
                table.Cell().Text(k.LevertAv.Visningsnavn());

                if (visPris)
                {
                    var utpris = visPrisPaLinje ? hentUtpris(k) : 0m;
                    if (visPrisPaLinje)
                    {
                        sumVarer += utpris * k.Antall;
                    }

                    table.Cell().Text(visPrisPaLinje ? FormatKr(utpris) : "–");
                    table.Cell().Text(visPrisPaLinje ? FormatKr(utpris * k.Antall) : "–");
                }
            }
        });

        if (visPris)
        {
            col.Item().AlignRight().PaddingTop(3).Text($"Sum varer: {FormatKr(sumVarer)}").Bold().FontSize(9);
        }

        if (!string.IsNullOrWhiteSpace(dor.Notater))
        {
            col.Item().PaddingTop(10).Text("Merknad").FontSize(9).Bold();
            col.Item().PaddingTop(2).Text(dor.Notater).FontSize(9);
        }
    }

    private static string FormatKr(decimal value) => value.ToString("N2", Kultur) + " kr";
}
