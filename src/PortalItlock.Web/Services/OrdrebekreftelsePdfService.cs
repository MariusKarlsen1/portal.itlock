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
// Samme PDF lages også fra en arbeidsordre direkte på ordren (uten tilbud),
// fra ordrelinjene, og lagres på arbeidsordren (EntityTypeArbeidsordre).
public class OrdrebekreftelsePdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    public const string EntityType = "Ordrebekreftelse";
    public const string EntityTypeArbeidsordre = "ArbeidsordreOrdrebekreftelse";
    private static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");
    private static readonly Color Ink = Color.FromHex("#292927");
    private static readonly Color Svak = Color.FromHex("#6D675F");
    private static readonly Color Linje = Color.FromHex("#D9D6D0");
    private static readonly Color RadGra = Color.FromHex("#F0F0F0");
    private const decimal MvaSats = 0.25m;

    public sealed record Resultat(byte[] Pdf, string Ordrenummer, int Versjon);

    // Én linje i tabellen. Rabatt er null når linjen ikke har rabatt (kolonnen står tom).
    private sealed record PdfLinje(string Nummer, string Navn, string? Under, string Antall, decimal Pris, decimal? Rabatt, decimal Total);

    private sealed record Innhold(
        string Ordrenummer, string? Erstatter, List<string> KundeLinjer, List<string> LeveringLinjer,
        List<(string Nokkel, string? Verdi)> Opplysninger, List<PdfLinje> Linjer, decimal TotaltEks,
        bool ErPrivat, string? VarReferanse, string? KontaktEpost, ObMalInnhold Mal);

    // Malen fra Oppsett og maler → Ordrebekreftelse (standardteksten hvis den ikke er endret).
    private async Task<ObMalInnhold> MalAsync() =>
        ObMalInnhold.Les((await db.OrdrebekreftelseMaler.AsNoTracking().FirstOrDefaultAsync())?.Json);

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

        var varReferanse = prosjekt?.Prosjektleder?.Navn ?? bruker?.Navn ?? tilbud.OpprettetAvNavn;
        var harRabatt = varer.Any(l => l.RabattProsent > 0 && l.PrisVeiledende is > 0);
        var linjer = varer.Select(l =>
        {
            var medRabatt = harRabatt && l.RabattProsent > 0 && l.PrisVeiledende is > 0;
            return new PdfLinje(
                l.Component?.Produktkode ?? "", l.Navn,
                string.Join(" · ", new[] { l.Component?.Overflate, l.Component?.Leverandor }.Where(s => !string.IsNullOrWhiteSpace(s))),
                $"{l.Antall}{(string.IsNullOrWhiteSpace(l.Enhet) || l.Enhet.Equals("stk", StringComparison.OrdinalIgnoreCase) ? "" : " " + l.Enhet)}",
                medRabatt ? l.PrisVeiledende!.Value : l.Utpris, medRabatt ? l.RabattProsent : null, l.Utpris * l.Antall);
        }).ToList();
        linjer.AddRange(arbeid.Select(a => new PdfLinje(a.Kode, a.Navn, null, "1", a.Belop, null, a.Belop)));

        var innhold = new Innhold(
            ordrenummer,
            versjon > 1 ? $"{tilbud.OpprettetDato.Year}-{tilbud.Id:000}{(versjon > 2 ? $"-{versjon - 1}" : "")}" : null,
            KundeLinjer(kunde),
            LeveringLinjer(kunde, tilbud.KontaktpersonNavn, prosjekt?.Adresse, prosjekt?.Postnr, prosjekt?.Sted),
            [
                ("kundenr", kunde?.TripletexKundenummer ?? kunde?.Id.ToString()),
                ("deres", tilbud.KontaktpersonNavn ?? kunde?.Kontaktperson),
                ("var", varReferanse),
                ("levmate", arbeid.Count > 0 ? "Levering og montering" : "Levering"),
                ("levtid", string.IsNullOrWhiteSpace(tilbud.Leveringstid) ? null : tilbud.Leveringstid),
                ("betaling", null),
                ("tilbud", $"T-{tilbud.OpprettetDato.Year}-{tilbud.Id:000}"),
                ("godkjent", (tilbud.GodkjentDato ?? DateTime.Today).ToString("dd.MM.yyyy")),
                ("prosjektid", prosjekt is null ? null : $"P-{prosjekt.Id}"),
                ("prosjektnavn", prosjekt?.Navn),
            ],
            linjer, totaltEks, kunde?.Type == KundeType.Privatperson, varReferanse,
            prosjekt?.Prosjektleder?.Epost ?? org.Epost, await MalAsync());

        return new Resultat(Tegn(innhold, org), ordrenummer, versjon);
    }

    // Ordrebekreftelse fra en arbeidsordre direkte på ordren: varene på ordren
    // og arbeidet (førte timer og kjøring, med eventuelle overstyringer).
    public async Task<Resultat?> LagForArbeidsordreAsync(int arbeidsordreId, int? brukerId)
    {
        var ordre = await db.Arbeidsordre
            .Include(a => a.Kunde)
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Kunde)
            .Include(a => a.Prosjekt).ThenInclude(p => p!.Prosjektleder)
            .Include(a => a.Varer).ThenInclude(v => v.Component)
            .Include(a => a.KodeLinjer)
            .Include(a => a.Timeregistreringer)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == arbeidsordreId);
        if (ordre is null)
        {
            return null;
        }

        var koder = await db.PrisKoder.AsNoTracking().ToListAsync();
        var tidligere = await db.LagredePdfer.CountAsync(p => p.EntityType == EntityTypeArbeidsordre && p.EntityId == arbeidsordreId);
        var versjon = tidligere + 1;
        var grunn = $"AO-{ordre.Id}";
        var ordrenummer = grunn + (versjon > 1 ? $"-{versjon}" : "");
        var bruker = brukerId is int bid ? await db.Brukere.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bid) : null;

        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = ordre.Kunde ?? ordre.Prosjekt?.Kunde;
        var arbeid = ArbeidsordreOkonomiBeregner.DirekteArbeidLinjer(ordre, koder);
        var linjer = ordre.Varer.Where(v => v.Antall != 0).OrderBy(v => v.Id).Select(v =>
        {
            var medRabatt = v.RabattProsent > 0 && v.Listepris is > 0;
            return new PdfLinje(v.Component?.Produktkode ?? "", v.Navn,
                string.Join(" · ", new[] { v.Component?.Overflate, v.Component?.Leverandor }.Where(s => !string.IsNullOrWhiteSpace(s))),
                v.Antall.ToString(Kultur), medRabatt ? v.Listepris!.Value : v.Utpris, medRabatt ? v.RabattProsent : null, v.Utpris * v.Antall);
        }).ToList();
        linjer.AddRange(arbeid.Where(a => a.Sum != 0).Select(a => new PdfLinje(a.Kode, a.Navn, null,
            a.Antall.ToString("0.##", Kultur), a.Utpris, a.Rabatt > 0 ? a.Rabatt : null, a.Sum)));
        var totaltEks = linjer.Sum(l => l.Total);
        var varReferanse = ordre.Prosjekt?.Prosjektleder?.Navn ?? bruker?.Navn;

        var innhold = new Innhold(
            ordrenummer,
            versjon > 1 ? grunn + (versjon > 2 ? $"-{versjon - 1}" : "") : null,
            KundeLinjer(kunde),
            LeveringLinjer(kunde, null, ordre.Adresse ?? ordre.Prosjekt?.Adresse, ordre.Postnr ?? ordre.Prosjekt?.Postnr, ordre.Sted ?? ordre.Prosjekt?.Sted),
            [
                ("kundenr", kunde?.TripletexKundenummer ?? kunde?.Id.ToString()),
                ("deres", kunde?.Kontaktperson),
                ("var", varReferanse),
                ("levmate", arbeid.Count > 0 ? "Levering og montering" : "Levering"),
                ("levtid", ordre.PlanlagtDato is { } d ? d.ToString("dd.MM.yyyy") : null),
                ("betaling", null),
                ("ordre", $"#{ordre.Id} {ordre.Tittel}"),
                ("prosjektid", ordre.Prosjekt is null ? null : $"P-{ordre.Prosjekt.Id}"),
                ("prosjektnavn", ordre.Prosjekt?.Navn),
            ],
            linjer, totaltEks, kunde?.Type == KundeType.Privatperson, varReferanse,
            ordre.Prosjekt?.Prosjektleder?.Epost ?? bruker?.Epost ?? org.Epost, await MalAsync());

        return new Resultat(Tegn(innhold, org), ordrenummer, versjon);
    }

    // Eksempel med oppdiktede linjer, til «Lagre og forhåndsvis» på malen.
    public async Task<byte[]> LagEksempelAsync(bool privat)
    {
        var org = RapportProfil.Fra(tenantContext.Current);
        var kunde = new Kunde { Navn = privat ? "Ola Nordmann" : "Eksempel AS", Adresse = "Storgata 1", Postnr = "4370", Sted = "Egersund", Kontaktperson = privat ? null : "Kari Kunde", Type = privat ? KundeType.Privatperson : KundeType.Bedriftskunde };
        var linjer = new List<PdfLinje>
        {
            new("AK5.10", "Farget plastlokk S5 for nøkkel (K5S) Mørk grå", "iLOQ", "1", 673, null, 673),
            new("K55S.2", "Nøkkel-Fob for iLOQ 5-serien låssystem", null, "1", 1126, 30, 788.20m),
            new("HF1", "Ekspedisjonsgebyr", null, "1", 123, null, 123),
            new("TA100", "Montasje", null, "3,5", 1100, null, 3850),
        };
        var innhold = new Innhold("AO-123", null, KundeLinjer(kunde), LeveringLinjer(kunde, null, null, null, null),
            [("kundenr", "10002"), ("deres", kunde.Kontaktperson), ("var", "Marius Karlsen"), ("levmate", "Levering og montering"),
             ("levtid", null), ("betaling", null), ("ordre", "#123 Bytte låskasser"), ("tilbud", null), ("godkjent", null), ("prosjektid", null), ("prosjektnavn", null)],
            linjer, linjer.Sum(l => l.Total), privat, "Marius Karlsen", org.Epost, await MalAsync());
        return Tegn(innhold, org);
    }

    private static List<string> KundeLinjer(Kunde? kunde) =>
        new List<string?> { kunde?.Navn, kunde?.Adresse, string.Join(" ", new[] { kunde?.Postnr, kunde?.Sted }.Where(s => !string.IsNullOrWhiteSpace(s))), "Norge" }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();

    private static List<string> LeveringLinjer(Kunde? kunde, string? kontakt, string? adresse, string? postnr, string? sted) =>
        new List<string?> { kunde?.Navn, kontakt ?? kunde?.Kontaktperson,
                adresse ?? kunde?.Adresse,
                string.Join(" ", new[] { postnr ?? kunde?.Postnr, (sted ?? kunde?.Sted)?.ToUpperInvariant() }.Where(s => !string.IsNullOrWhiteSpace(s))), "Norge" }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();

    private byte[] Tegn(Innhold i, RapportProfil org)
    {
        var mva = Math.Round(i.TotaltEks * MvaSats, 2);
        void Stiplet(IContainer c) => c.LineHorizontal(1).LineColor(Ink).LineDashPattern([5f, 4f]);

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(1.6f, Unit.Centimetre);
            page.MarginVertical(1.2f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

            // Bunn som i malen: stiplet linje, firma / kontakt / web, og leveringsvilkår
            page.Footer().Column(f =>
            {
                f.Item().PaddingBottom(8).Element(Stiplet);
                f.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text(org.Navn);
                        foreach (var del in (org.Adresse ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            c.Item().Text(del);
                        }
                    });
                    r.RelativeItem().Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.ConstantColumn(55); cd.RelativeColumn(); });
                        void R(string a, string? b) { if (string.IsNullOrWhiteSpace(b)) return; t.Cell().Text(a); t.Cell().Text(b); }
                        R("Tlf.", org.Telefon);
                        R("Org.nr", org.OrgNr);
                        R("E-post", org.Epost);
                    });
                });
                if (!string.IsNullOrWhiteSpace(i.Mal.Bunntekst))
                {
                    f.Item().PaddingTop(8).Text(i.Mal.Bunntekst.Replace(ObMalInnhold.Firma, org.Navn)).FontSize(8);
                }
            });

            page.Header().AlignRight().Text(t =>
            {
                t.Span("Side ");
                t.CurrentPageNumber();
                t.Span("/");
                t.TotalPages();
            });

            page.Content().Column(col =>
            {
                // Logo til venstre, ORDREBEKREFTELSE med ordrenr/dato til høyre
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.ConstantItem(150).PaddingTop(4).Element(e => org.TegnLogo(e, pdfLogo, 34));
                    row.RelativeItem();
                    row.ConstantItem(230).Column(c =>
                    {
                        c.Item().Text("ORDREBEKREFTELSE").FontSize(17).Bold();
                        c.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.ConstantColumn(110); cd.RelativeColumn(); });
                            void R(string a, string b) { t.Cell().Text(a); t.Cell().Text(b); }
                            R("Ordrenummer", i.Ordrenummer);
                            R("Dato", DateTime.Today.ToString("dd.MM.yyyy"));
                            if (i.Erstatter is not null)
                            {
                                R("Erstatter", i.Erstatter);
                            }
                        });
                    });
                });

                // Kunde og leveringsadresse til venstre, opplysninger til høyre
                col.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        foreach (var l in i.KundeLinjer) c.Item().Text(l);
                        c.Item().PaddingTop(16).Text("Leveringsadresse").Bold();
                        foreach (var l in i.LeveringLinjer) c.Item().Text(l);
                    });
                    row.RelativeItem(1.25f).Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.ConstantColumn(150); cd.RelativeColumn(); });
                        // Feltene, rekkefølgen og standardverdiene kommer fra malen. Felt uten
                        // verdi tas ikke med. Etiketten står i fet skrift.
                        var verdier = i.Opplysninger.GroupBy(x => x.Nokkel).ToDictionary(g => g.Key, g => g.First().Verdi);
                        foreach (var felt in i.Mal.Felt.Where(f => f.Vis && verdier.ContainsKey(f.Nokkel)))
                        {
                            var verdi = verdier[felt.Nokkel];
                            // Egen tekst i malen går foran (leveringsmåte, betaling); ellers er den standard når feltet er tomt.
                            if (felt.Nokkel is "levmate" or "betaling" && !string.IsNullOrWhiteSpace(felt.Verdi))
                            {
                                verdi = felt.Verdi;
                            }
                            verdi = string.IsNullOrWhiteSpace(verdi) ? felt.Verdi : verdi;
                            if (string.IsNullOrWhiteSpace(verdi))
                            {
                                continue;
                            }
                            t.Cell().PaddingBottom(1).Text(felt.Navn).Bold();
                            t.Cell().PaddingBottom(1).Text(verdi);
                        }
                    });
                });

                // Varelinjer mellom stiplede linjer
                col.Item().PaddingTop(40).Element(Stiplet);
                col.Item().PaddingTop(2).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(95);
                        cd.RelativeColumn(3);
                        cd.ConstantColumn(55);
                        cd.ConstantColumn(80);
                        cd.ConstantColumn(50);
                        cd.ConstantColumn(80);
                    });
                    t.Header(h =>
                    {
                        IContainer H(IContainer c) => c.BorderBottom(0.75f).BorderColor(Ink).PaddingVertical(5).PaddingHorizontal(3);
                        h.Cell().Element(H).Text("Nummer").Bold();
                        h.Cell().Element(H).Text("Beskrivelse").Bold();
                        h.Cell().Element(H).AlignRight().Text("Antall").Bold();
                        h.Cell().Element(H).AlignRight().Text("Pris pr enhet").Bold();
                        h.Cell().Element(H).AlignRight().Text("Rabatt %").Bold();
                        h.Cell().Element(H).AlignRight().Text("Totalt").Bold();
                    });
                    // Annenhver grå og hvit rad, så linjene er lette å følge.
                    var radNr = 0;
                    foreach (var l in i.Linjer)
                    {
                        var gra = radNr++ % 2 == 0;
                        IContainer C(IContainer c) => (gra ? c.Background(RadGra) : c).PaddingVertical(5).PaddingHorizontal(3);
                        t.Cell().Element(C).Text(l.Nummer);
                        t.Cell().Element(C).Column(c =>
                        {
                            c.Item().Text(l.Navn);
                            if (!string.IsNullOrWhiteSpace(l.Under)) c.Item().Text(l.Under).FontSize(8);
                        });
                        t.Cell().Element(C).AlignRight().Text(l.Antall);
                        t.Cell().Element(C).AlignRight().Text(Kr(l.Pris));
                        t.Cell().Element(C).AlignRight().Text(l.Rabatt is { } r ? r.ToString("0.#", Kultur) : "");
                        t.Cell().Element(C).AlignRight().Text(Kr(l.Total));
                    }
                });

                // Sum som i malen: kort strek over beløpet, «Totalt NOK»
                col.Item().PaddingTop(14).AlignRight().Width(250).Table(t =>
                {
                    t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(90); });
                    t.Cell().PaddingVertical(1).AlignRight().PaddingRight(12).Text("Sum eks. mva");
                    t.Cell().PaddingVertical(1).AlignRight().Text(Kr(i.TotaltEks));
                    t.Cell().PaddingVertical(1).AlignRight().PaddingRight(12).Text("Mva 25 %");
                    t.Cell().PaddingVertical(1).AlignRight().Text(Kr(mva));
                    t.Cell();
                    t.Cell().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Black);
                    t.Cell().PaddingTop(4).AlignRight().PaddingRight(12).Text("Totalt NOK").Bold();
                    t.Cell().PaddingTop(4).AlignRight().Text(Kr(i.TotaltEks + mva)).Bold();
                });

                if (!string.IsNullOrWhiteSpace(i.Mal.Takk))
                {
                    col.Item().PaddingTop(24).Text(i.Mal.Takk.Replace(ObMalInnhold.Firma, org.Navn));
                }

                // Betingelser fra malen. Punkter merket «kun privat» tas bare med for privatkunder.
                var punkter = i.Mal.Punkter.Where(p => p.Vis && (!p.KunPrivat || i.ErPrivat) && !string.IsNullOrWhiteSpace(p.Tekst)).ToList();
                if (punkter.Count > 0)
                {
                    col.Item().PaddingTop(16).Text("Betingelser").Bold();
                    foreach (var p in punkter)
                    {
                        var tekst = p.Tekst.Replace(ObMalInnhold.Firma, org.Navn);
                        Punkt(col, string.IsNullOrWhiteSpace(p.Tittel) ? tekst : $"{p.Tittel}: {tekst}");
                    }
                }
                if (i.Mal.Kontakt)
                {
                    var kontakt = string.Join(" · ", new[] { i.VarReferanse, org.Telefon, i.KontaktEpost }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    col.Item().PaddingTop(12).Text($"Spørsmål eller feil i ordren? Ta kontakt{(kontakt.Length > 0 ? ": " + kontakt : ".")}");
                }
            });
        })).GeneratePdf();
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
