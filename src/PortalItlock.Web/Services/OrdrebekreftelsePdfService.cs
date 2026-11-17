using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// Ordrebekreftelse som lages automatisk når et tilbud blir godkjent. Lagres på
// tilbudet (LagretPdf, EntityType «Ordrebekreftelse») - sendes ikke på e-post.
// Oppsett som en vanlig ordrebekreftelse (logo, ordrenr/dato, kunde og
// leveringsadresse, referanser, varelinjer, sum) + levering/oppmøte,
// salgspant og avtalebetingelser. PDF-stil N1: hvit topp og tynne linjer.
public class OrdrebekreftelsePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public const string EntityType = "Ordrebekreftelse";
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");
    private static readonly Color Ink = Color.FromHex("#292927");
    private static readonly Color Svak = Color.FromHex("#6D675F");
    private static readonly Color Linje = Color.FromHex("#D9D6D0");
    private const decimal MvaSats = 0.25m;

    public sealed record Resultat(byte[] Pdf, string Ordrenummer, int Versjon);

    public async Task<Resultat?> LagAsync(int tilbudId, int? brukerId)
    {
        var tilbud = await db.Tilbud
            .Include(t => t.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(t => t.Prosjekt).ThenInclude(p => p!.Prosjektleder)
            .Include(t => t.Linjer).ThenInclude(l => l.Component)
            .Include(t => t.KodeLinjer)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tilbudId);
        if (tilbud is null)
        {
            return null;
        }

        var tidligere = await db.LagredePdfer.CountAsync(p => p.EntityType == EntityType && p.EntityId == tilbudId);
        var versjon = tidligere + 1;
        var ordrenummer = $"{tilbud.OpprettetDato.Year}-{tilbud.Id:000}" + (versjon > 1 ? $"-{versjon}" : "");
        var bruker = brukerId is int bid ? await db.Brukere.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bid) : null;

        var org = RapportProfil.Fra(tenantContext.Current);
        var prosjekt = tilbud.Prosjekt;
        var kunde = prosjekt?.Kunde;
        var erPrivat = kunde?.Type == KundeType.Privatperson;
        var varer = tilbud.Linjer.Where(l => l.LevertAv == LevertAv.F && !l.ErGruppering && l.Antall != 0)
            .OrderBy(l => l.Rekkefolge).ToList();
        var sumVarer = varer.Sum(l => l.Utpris * l.Antall);
        var totaltEks = TilbudBeregningHelper.BeregnTotaltUtenMva(tilbud);
        var arbeid = new List<(string Kode, string Navn, decimal Belop)>();
        if (tilbud.BrukerKodelinjer && tilbud.KodeLinjer.Count > 0)
        {
            arbeid = tilbud.KodeLinjer.OrderBy(l => l.Rekkefolge).ThenBy(l => l.Id)
                .Select(l => (l.Kode, l.Navn, KodeLinjeBeregner.Sum(l, tilbud.KodeLinjer, sumVarer).Salg))
                .Where(x => x.Item3 != 0).ToList();
        }
        else if (totaltEks - sumVarer != 0)
        {
            arbeid.Add(("", "Montering og arbeid", totaltEks - sumVarer));
        }
        var mva = Math.Round(totaltEks * MvaSats, 2);

        string? Adr(string? a, string? postnr, string? sted)
        {
            var linje2 = string.Join(" ", new[] { postnr, sted }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var deler = new[] { a, linje2 }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return deler.Count == 0 ? null : string.Join("\n", deler);
        }
        var kundeAdresse = kunde is null ? null : Adr(kunde.Adresse, kunde.Postnr, kunde.Sted);
        var leveringsadresse = prosjekt is null ? null : Adr(prosjekt.Adresse, prosjekt.Postnr, prosjekt.Sted);
        var varReferanse = prosjekt?.Prosjektleder?.Navn ?? bruker?.Navn ?? tilbud.OpprettetAvNavn;
        var kontaktEpost = prosjekt?.Prosjektleder?.Epost ?? org.Epost;

        var pdf = Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.8f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Ink));
            page.Footer().Element(f => RapportMal.Bunn(f, org));

            page.Content().Column(col =>
            {
                // Topp: logo til venstre, tittel og ordrenummer/dato
                col.Item().Row(row =>
                {
                    row.ConstantItem(170).Element(e => org.TegnLogo(e, pdfLogo, 30));
                    row.RelativeItem().PaddingLeft(20).Column(c =>
                    {
                        c.Item().Text("ORDREBEKREFTELSE").FontSize(20).Bold();
                        c.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.ConstantColumn(95); cd.RelativeColumn(); });
                            void R(string a, string b) { t.Cell().Text(a).FontColor(Svak); t.Cell().Text(b).SemiBold(); }
                            R("Ordrenummer", ordrenummer);
                            R("Dato", DateTime.Today.ToString("dd.MM.yyyy"));
                            if (versjon > 1)
                            {
                                R("Erstatter", $"Ordrebekreftelse {tilbud.OpprettetDato.Year}-{tilbud.Id:000}{(versjon > 2 ? $"-{versjon - 1}" : "")}");
                            }
                        });
                    });
                });

                // Kunde / levering til venstre, referanser til høyre
                col.Item().PaddingTop(26).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(kunde?.Navn ?? "-").SemiBold();
                        if (kundeAdresse is not null) c.Item().Text(kundeAdresse);
                        if (!string.IsNullOrWhiteSpace(tilbud.KontaktpersonNavn)) c.Item().Text($"Att: {tilbud.KontaktpersonNavn}");

                        if (leveringsadresse is not null && leveringsadresse != kundeAdresse)
                        {
                            c.Item().PaddingTop(14).Text("Leverings- og oppmøteadresse").Bold();
                            if (!string.IsNullOrWhiteSpace(prosjekt?.Navn)) c.Item().Text(prosjekt!.Navn);
                            c.Item().Text(leveringsadresse);
                        }
                    });
                    row.RelativeItem().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.ConstantColumn(130); cd.RelativeColumn(); });
                        void R(string a, string? b)
                        {
                            if (string.IsNullOrWhiteSpace(b)) return;
                            t.Cell().PaddingBottom(2).Text(a).FontColor(Svak);
                            t.Cell().PaddingBottom(2).Text(b);
                        }
                        R("Kundenummer", kunde?.TripletexKundenummer ?? kunde?.Id.ToString());
                        R("Deres referanse", tilbud.KontaktpersonNavn ?? kunde?.Kontaktperson);
                        R("Vår referanse", varReferanse);
                        R("Prosjekt", prosjekt?.Navn);
                        R("Tilbud", $"T-{tilbud.OpprettetDato.Year}-{tilbud.Id:000} · {tilbud.Tittel}");
                        R("Godkjent", (tilbud.GodkjentDato ?? DateTime.Today).ToString("dd.MM.yyyy"));
                        R("Leveringstid", string.IsNullOrWhiteSpace(tilbud.Leveringstid) ? "Avtales nærmere" : tilbud.Leveringstid);
                        R("Betalingsbetingelser", "Iht. faktura");
                    });
                });

                col.Item().PaddingTop(18).Text("Takk for din bestilling! Vi har mottatt ordren og startet behandlingen. Under finner du en oversikt over produkter, tjenester og levering.").FontColor(Ink);

                // Varelinjer
                col.Item().PaddingTop(16).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(80);
                        cd.RelativeColumn(4);
                        cd.ConstantColumn(55);
                        cd.ConstantColumn(80);
                        cd.ConstantColumn(85);
                    });
                    t.Header(h =>
                    {
                        IContainer H(IContainer c) => c.BorderBottom(1).BorderColor(Ink).PaddingBottom(4);
                        h.Cell().Element(H).Text("Nummer").Bold();
                        h.Cell().Element(H).Text("Beskrivelse").Bold();
                        h.Cell().Element(H).AlignRight().Text("Antall").Bold();
                        h.Cell().Element(H).AlignRight().Text("Pris eks. mva").Bold();
                        h.Cell().Element(H).AlignRight().Text("Totalt eks. mva").Bold();
                    });
                    IContainer C(IContainer c) => c.BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4);
                    foreach (var l in varer)
                    {
                        t.Cell().Element(C).Text(l.Component?.Produktkode ?? "").FontSize(8.5f);
                        t.Cell().Element(C).Column(c =>
                        {
                            c.Item().Text(l.Navn);
                            if (!string.IsNullOrWhiteSpace(l.Component?.Overflate)) c.Item().Text(l.Component!.Overflate).FontSize(8).FontColor(Svak);
                        });
                        t.Cell().Element(C).AlignRight().Text($"{l.Antall} {(string.IsNullOrWhiteSpace(l.Enhet) ? "stk" : l.Enhet)}");
                        t.Cell().Element(C).AlignRight().Text(Kr(l.Utpris));
                        t.Cell().Element(C).AlignRight().Text(Kr(l.Utpris * l.Antall));
                    }
                    foreach (var (kode, navn, belop) in arbeid)
                    {
                        t.Cell().Element(C).Text(kode).FontSize(8.5f);
                        t.Cell().Element(C).Text(navn);
                        t.Cell().Element(C).AlignRight().Text("1");
                        t.Cell().Element(C).AlignRight().Text(Kr(belop));
                        t.Cell().Element(C).AlignRight().Text(Kr(belop));
                    }
                });

                // Sum
                col.Item().PaddingTop(8).AlignRight().Width(240).Table(t =>
                {
                    t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(95); });
                    t.Cell().PaddingVertical(2).Text("Sum eks. mva").FontColor(Svak);
                    t.Cell().PaddingVertical(2).AlignRight().Text(Kr(totaltEks));
                    t.Cell().PaddingVertical(2).Text("Merverdiavgift 25 %").FontColor(Svak);
                    t.Cell().PaddingVertical(2).AlignRight().Text(Kr(mva));
                    t.Cell().BorderTop(1).BorderColor(Ink).PaddingTop(4).Text("Totalt inkl. mva").Bold();
                    t.Cell().BorderTop(1).BorderColor(Ink).PaddingTop(4).AlignRight().Text(Kr(totaltEks + mva)).Bold();
                });

                // Levering og oppmøte
                Seksjon(col, "Levering og oppmøte");
                Punkt(col, $"Adresse for oppmøte og levering: {(leveringsadresse ?? kundeAdresse ?? "avtales nærmere").Replace("\n", ", ")}");
                Punkt(col, $"Tidspunkt for montering avtales nærmere. Montøren ringer før ankomst. Sørg for at det er ryddet og fri tilgang til døren/området.");
                Punkt(col, $"Estimert leveringstid: {(string.IsNullOrWhiteSpace(tilbud.Leveringstid) ? "avtales nærmere" : tilbud.Leveringstid)}.");

                // Salgspant og avtalebetingelser
                Seksjon(col, "Salgspant og avtalebetingelser");
                Punkt(col, $"Salgspant og eiendomsrett: {org.Navn} forbeholder seg salgspant i de leverte varene som sikkerhet for kjøpesummen, inkludert renter og omkostninger, i medhold av panteloven §§ 3-14 til 3-22. Eiendomsretten går over til kjøper først når hele kjøpesummen er betalt.");
                Punkt(col, "Spesialtilpassede varer: Det gis ikke retur- eller avbestillingsrett på spesialtilpassede systemlåser, lisensierte nøkler eller prosjektbestilte låsvarer etter at ordren er satt i produksjon eller bekreftet bestilt hos produsent.");
                Punkt(col, "Avbestilling av tjenester: Avbestilling eller endring av avtalt montering må skje senest 24 timer før oppmøte. Ved for sen avbestilling eller manglende tilgang påløper bomtur etter gjeldende satser.");
                if (erPrivat)
                {
                    Punkt(col, "Angrerett (forbrukerkjøp): Det er 14 dagers angrerett på standard lagervarer i ubrukt stand og originalemballasje. Angreretten bortfaller for tjenester (som montering) når arbeidet er påbegynt eller fullført etter avtale.");
                }

                // Kundeservice
                Seksjon(col, "Spørsmål om ordren");
                var kontakt = string.Join(" · ", new[] { varReferanse, org.Telefon, kontaktEpost }.Where(s => !string.IsNullOrWhiteSpace(s)));
                col.Item().PaddingTop(2).Text($"Har du spørsmål eller ser du feil i ordren? Ta kontakt med oss{(kontakt.Length > 0 ? ": " + kontakt : ".")}");
            });
        })).GeneratePdf();

        return new Resultat(pdf, ordrenummer, versjon);
    }

    private static void Seksjon(ColumnDescriptor col, string tittel) =>
        col.Item().PaddingTop(16).BorderBottom(0.5f).BorderColor(Linje).PaddingBottom(3).Text(tittel).FontSize(10.5f).Bold();

    private static void Punkt(ColumnDescriptor col, string tekst) =>
        col.Item().PaddingTop(4).Row(r =>
        {
            r.ConstantItem(10).Text("•");
            r.RelativeItem().Text(tekst).FontSize(9);
        });

    private static string Kr(decimal v) => v.ToString("N2", Kultur) + " kr";
}
