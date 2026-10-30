using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Innkjøpsordre som sendes til leverandør - organisasjonens logo og farge,
// leveringsadresse, ønsket leveringsdato, varelinjer med leverandørens
// varenummer og sum eks. mva.
public class InnkjopsordrePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public async Task<byte[]?> GenerateAsync(int ordreId)
    {
        var ordre = await db.Innkjopsordrer.AsNoTracking()
            .Include(o => o.Linjer)
            .Include(o => o.Leverandor)
            .Include(o => o.Lager)
            .Include(o => o.Prosjekt)
            .FirstOrDefaultAsync(o => o.Id == ordreId);
        if (ordre is null)
        {
            return null;
        }

        var org = RapportProfil.Fra(tenantContext.Current);
        var lev = ordre.Leverandor;
        var levAdresse = lev is null ? null : string.Join(", ", new[] { lev.Adresse, string.Join(" ", new[] { lev.Postnr, lev.Sted }.Where(s => !string.IsNullOrWhiteSpace(s))) }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var sum = ordre.Linjer.Sum(l => l.Antall * l.Pris);

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                RapportMal.Side(page);

                page.Header().Column(col =>
                {
                    var hoyre = $"{ordre.Visningsnummer} · {(ordre.Sendt ?? ordre.Opprettet):dd.MM.yyyy}";
                    col.Item().ShowOnce().Element(e => RapportMal.Band(e, org, pdfLogo, "Innkjøpsordre", ordre.LeverandorNavn, null, null, hoyre));
                    col.Item().SkipOnce().Element(e => RapportMal.Lopetopp(e, org, "Innkjøpsordre", hoyre));
                });

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Row(row =>
                    {
                        row.Spacing(14);
                        row.RelativeItem().Element(e => Adresseblokk(e, org, "Leverandør", ordre.LeverandorNavn,
                            levAdresse, lev?.Kontaktperson is { Length: > 0 } k ? $"Att: {k}" : null, ordre.LeverandorEpost ?? lev?.Epost));
                        row.RelativeItem().Element(e => Adresseblokk(e, org, "Leveres til", org.Navn,
                            org.Adresse, ordre.Lager is { } l && l.Type != LagerType.Hovedlager ? l.Navn : null,
                            org.OrgNr is null ? null : $"Org.nr {org.OrgNr}"));
                    });

                    RapportMal.InfoBoks(col, org,
                        ("Ønsket levering", ordre.OnsketLevering?.ToString("dd.MM.yyyy") ?? "Snarest"),
                        ("Vår referanse", ordre.VarReferanse ?? ordre.OpprettetAv),
                        ("Prosjekt", ordre.Prosjekt?.Navn),
                        ("Kontakt", string.Join(" · ", new[] { org.Telefon, org.Epost }.Where(s => !string.IsNullOrWhiteSpace(s)))));

                    col.Item().Column(c =>
                    {
                        RapportMal.Overskrift(c, "Varer", org);
                        c.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(80);
                                cd.RelativeColumn();
                                cd.ConstantColumn(50);
                                cd.ConstantColumn(70);
                                cd.ConstantColumn(80);
                            });
                            table.Header(h =>
                            {
                                foreach (var (tekst, hoyre) in new[] { ("Varenr", false), ("Beskrivelse", false), ("Antall", true), ("Pris", true), ("Sum", true) })
                                {
                                    var celle = h.Cell().BorderBottom(1.2f).BorderColor(RapportMal.Linje).PaddingVertical(4).PaddingRight(hoyre ? 0 : 6);
                                    (hoyre ? celle.AlignRight() : celle).Text(tekst.ToUpperInvariant()).FontSize(7).SemiBold().FontColor(RapportMal.Svak);
                                }
                            });
                            foreach (var l in ordre.Linjer.OrderBy(l => l.Id))
                            {
                                Celle(table).Text(l.Varenummer ?? "–");
                                Celle(table).Text(l.Navn);
                                Celle(table).AlignRight().Text($"{l.Antall} {l.Enhet ?? "stk"}");
                                Celle(table).AlignRight().Text(Kr(l.Pris));
                                Celle(table).AlignRight().Text(Kr(l.Antall * l.Pris));
                            }
                        });
                        c.Item().PaddingTop(6).AlignRight().Text(t =>
                        {
                            t.Span("Sum eks. mva  ").FontSize(9).FontColor(RapportMal.Dempet);
                            t.Span(Kr(sum)).FontSize(11).Bold();
                        });
                    });

                    RapportMal.Seksjon(col, "Merknad", org, ordre.Notat);

                    col.Item().Background(org.AksentLys).Padding(10).Text(t =>
                    {
                        t.Span("Vennligst send ordrebekreftelse med pris og leveringsdato til ").FontSize(9);
                        t.Span(org.Epost ?? "oss").FontSize(9).Bold();
                        t.Span($", og merk pakkseddel og faktura med {ordre.Visningsnummer}.").FontSize(9);
                    });
                });

                page.Footer().Element(e => RapportMal.Bunn(e, org));
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer Celle(TableDescriptor table) =>
        table.Cell().BorderBottom(0.5f).BorderColor(RapportMal.Linje).PaddingVertical(4).PaddingRight(6);

    private static string Kr(decimal v) => v.ToString("#,##0.00", Kultur);

    private static void Adresseblokk(IContainer container, RapportProfil org, string tittel, string navn, params string?[] linjer)
    {
        container.Border(0.75f).BorderColor(RapportMal.Linje).Padding(10).Column(c =>
        {
            c.Item().Text(tittel.ToUpperInvariant()).FontSize(6.8f).SemiBold().FontColor(RapportMal.Svak).LetterSpacing(0.05f);
            c.Item().PaddingTop(2).Text(navn).FontSize(10).Bold();
            foreach (var linje in linjer.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                c.Item().Text(linje!).FontSize(9).FontColor(RapportMal.Dempet);
            }
        });
    }
}
