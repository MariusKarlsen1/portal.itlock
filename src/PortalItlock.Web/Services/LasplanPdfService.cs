using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

public class LasplanPdfService(ApplicationDbContext db, PdfLogo pdfLogo, ITenantContext tenantContext)
{
    // Så mange nøkkelkolonner får plass i bredden på liggende A4 ved siden av dørkolonnene.
    private const int MaksNoklerPerSide = 20;

    public async Task<byte[]?> GenerateAsync(int prosjektId)
    {
        var prosjekt = await db.Prosjekter.Include(p => p.Kunde).FirstOrDefaultAsync(p => p.Id == prosjektId);
        if (prosjekt is null)
        {
            return null;
        }

        var nokler = await db.Nokler.Where(n => n.ProsjektId == prosjektId).OrderBy(n => n.Rekkefolge).ToListAsync();

        var dorKomponenter = await db.DorKomponenter
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.Component!.ErSylinder)
            .Include(k => k.Dor)
            .Include(k => k.Component)
            .OrderBy(k => k.Dor!.Dornummer)
            .ToListAsync();

        var reserver = await db.LasplanReserver
            .Where(r => r.ProsjektId == prosjektId)
            .Include(r => r.Component)
            .OrderBy(r => r.Id)
            .ToListAsync();

        if (dorKomponenter.Count == 0 && reserver.Count == 0)
        {
            return null;
        }

        var rader = new List<(string Nummer, string DorTil, string Dornr, string Sylindertype, int ComponentId, int? DorId, int? ReserveId)>();
        var nummer = 0;
        foreach (var dk in dorKomponenter)
        {
            nummer++;
            for (var i = 0; i < dk.Antall; i++)
            {
                rader.Add((i == 0 ? nummer.ToString() : $"{nummer}.{i}", dk.Dor!.DorTil ?? "", dk.Dor.Dornummer, dk.Component!.Navn, dk.ComponentId, dk.DorId, null));
            }
        }

        foreach (var res in reserver)
        {
            nummer++;
            var dorTil = string.IsNullOrWhiteSpace(res.DorTil) ? "–" : res.DorTil;
            var dornr = string.IsNullOrWhiteSpace(res.Dornr) ? "Reserve" : res.Dornr;
            for (var i = 0; i < res.Antall; i++)
            {
                rader.Add((i == 0 ? nummer.ToString() : $"{nummer}.{i}", dorTil, dornr, res.Component!.Navn, res.ComponentId, null, res.Id));
            }
        }

        var nokkelIder = nokler.Select(n => n.Id).ToList();
        var krysser = await db.NokkelSylindere.Where(ns => nokkelIder.Contains(ns.NokkelId)).ToListAsync();
        var krysserSett = krysser.Select(k => (k.NokkelId, k.DorId, k.ComponentId)).ToHashSet();

        var reserveIder = reserver.Select(r => r.Id).ToList();
        var reserveKrysser = await db.NokkelLasplanReserver.Where(nr => nokkelIder.Contains(nr.NokkelId) && reserveIder.Contains(nr.LasplanReserveId)).ToListAsync();
        var reserveKrysserSett = reserveKrysser.Select(k => (k.NokkelId, k.LasplanReserveId)).ToHashSet();

        bool Apner(Nokkel n, (string Nummer, string DorTil, string Dornr, string Sylindertype, int ComponentId, int? DorId, int? ReserveId) rad) =>
            rad.ReserveId is not null
                ? reserveKrysserSett.Contains((n.Id, rad.ReserveId.Value))
                : krysserSett.Contains((n.Id, rad.DorId!.Value, rad.ComponentId));

        // Mal L20 (lasplan-pdf-forslag.html): logo og tittel i topp med strek i
        // organisasjonens farge, infoboks i venstre hjørne av matrisen med luft
        // ned til overskriftsraden, nøkkelnavn rotert med merking (K1 ...) og
        // antall, annenhver nøkkelkolonne tonet, skillelinje for hver 5. sylinder,
        // signaturlinjer og bunntekst fra organisasjonen.
        var org = RapportProfil.Fra(tenantContext.Current);
        var dempet = Color.FromHex("#6B6863");
        var svak = Color.FromHex("#8A857C");
        var lysLinje = Color.FromHex("#ECE4D6");
        var gruppeLinje = Color.FromHex("#D9CDB9");
        var sand = Color.FromHex("#FBF8F3");
        var rammeFarge = Color.FromHex("#E3DCCF");

        IContainer Tone(IContainer c, int k) => k % 2 == 0 ? c.Background(org.AksentLys) : c;

        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.3f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontColor(PdfStil.Ink));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().AlignBottom().Element(e => org.TegnLogo(e, pdfLogo, 22));
                        row.RelativeItem().AlignBottom().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("Låsplan").FontSize(13).Bold();
                            c.Item().AlignRight().Text(prosjekt.Navn).FontSize(8).FontColor(dempet);
                        });
                    });
                    col.Item().PaddingTop(5).LineHorizontal(1.5f).LineColor(org.Aksent);
                });

                page.Content().PaddingTop(9).Column(col =>
                {
                    // Får ikke alle nøklene plass i bredden, fortsetter de på en ny side
                    // med de samme sylinderradene (MaksNoklerPerSide nøkler per del).
                    var deler = nokler.Chunk(MaksNoklerPerSide).Select(d => d.ToList()).DefaultIfEmpty(new List<Nokkel>()).ToList();
                    for (var d = 0; d < deler.Count; d++)
                    {
                        var del = deler[d];
                        if (d > 0)
                        {
                            col.Item().PageBreak();
                        }
                        if (deler.Count > 1)
                        {
                            col.Item().PaddingBottom(5).Text($"Nøkler {del[0].Merking}–{del[^1].Merking} · del {d + 1} av {deler.Count}").FontSize(7.5f).SemiBold().FontColor(dempet);
                        }
                        Matrise(col.Item(), del);
                    }

                    col.Item().PaddingTop(30).ShowEntire().Row(row =>
                    {
                        row.Spacing(28);
                        foreach (var tekst in new[] { "Dato", "Godkjent systemeier", "Utarbeidet av" })
                        {
                            row.RelativeItem().BorderTop(0.6f).BorderColor(PdfStil.Ink).PaddingTop(3).Text(tekst).FontSize(7).FontColor(dempet);
                        }
                    });
                });

                void Matrise(IContainer container, List<Nokkel> nokler)
                {
                    container.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(118);
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(2.6f);
                            columns.ConstantColumn(26);
                            foreach (var _ in nokler)
                            {
                                columns.ConstantColumn(18);
                            }
                        });

                        table.Header(header =>
                        {
                            // Infoboks i hjørnet, med luft ned til streken over overskriftsraden.
                            header.Cell().BorderBottom(0.8f).BorderColor(org.Aksent).PaddingBottom(9)
                                .Border(0.6f).BorderColor(rammeFarge).Padding(5).Column(c =>
                                {
                                    c.Spacing(3);
                                    void Felt(string etikett, string? verdi) => c.Item().Column(f =>
                                    {
                                        f.Item().Text(etikett.ToUpperInvariant()).FontSize(5.5f).Bold().FontColor(org.Aksent).LetterSpacing(0.08f);
                                        f.Item().Text(string.IsNullOrWhiteSpace(verdi) ? "–" : verdi).FontSize(7.5f).SemiBold();
                                    });
                                    Felt("Låsplan systemnr.", prosjekt.LasplanSystemnr);
                                    Felt("Systemeier", prosjekt.Kunde?.Navn);
                                    Felt("Prosjektnummer", prosjekt.LasplanProsjektnummer);
                                    Felt("Utarbeidet av / dato", prosjekt.LasplanUtarbeidetAv);
                                });
                            header.Cell().ColumnSpan(4).BorderBottom(0.8f).BorderColor(org.Aksent);

                            for (var k = 0; k < nokler.Count; k++)
                            {
                                var n = nokler[k];
                                Tone(header.Cell().BorderBottom(0.8f).BorderColor(org.Aksent), k).AlignBottom().PaddingBottom(2).Column(c =>
                                {
                                    c.Item().Height(62).RotateLeft().AlignMiddle().Text(n.Navn).FontSize(7).SemiBold();
                                    c.Item().PaddingTop(2).AlignCenter().Text(n.Merking ?? "").FontSize(7).Bold().FontColor(org.Aksent);
                                });
                            }

                            IContainer Overskrift(IContainer c) => c.Background(sand).BorderBottom(0.6f).BorderColor(rammeFarge).PaddingVertical(3).PaddingHorizontal(4);
                            void Kol(string tekst, bool midt = false)
                            {
                                var celle = Overskrift(header.Cell());
                                (midt ? celle.AlignCenter() : celle).Text(tekst.ToUpperInvariant()).FontSize(6).Bold().FontColor(dempet).LetterSpacing(0.05f);
                            }
                            Kol("Syl.");
                            Kol("Dør til");
                            Kol("Dørnr.");
                            Kol("Sylindertype");
                            Kol("Ant.", true);
                            for (var k = 0; k < nokler.Count; k++)
                            {
                                // Antall nøkler står i samme rad som «Ant.»-overskriften.
                                Tone(header.Cell().Background(sand), k).BorderBottom(0.6f).BorderColor(rammeFarge).PaddingVertical(3).AlignCenter()
                                    .Text(nokler[k].Antall.ToString()).FontSize(6.5f).Bold();
                            }
                        });

                        for (var i = 0; i < rader.Count; i++)
                        {
                            var rad = rader[i];
                            var reserve = rad.ReserveId is not null;
                            var gruppeSlutt = (i + 1) % 5 == 0 && i < rader.Count - 1;
                            var grense = gruppeSlutt ? gruppeLinje : lysLinje;
                            var grenseTykk = gruppeSlutt ? 0.8f : 0.5f;
                            IContainer Rad(IContainer c) => c.BorderBottom(grenseTykk).BorderColor(grense).PaddingVertical(2.5f).PaddingHorizontal(4);

                            void Tekst(string tekst, bool fet = false, bool gra = false, bool midt = false)
                            {
                                var celle = Rad(table.Cell());
                                var t = (midt ? celle.AlignCenter() : celle).Text(tekst);
                                if (fet) t.Bold();
                                if (gra || reserve) t.FontColor(gra && !reserve ? dempet : svak);
                                if (reserve) t.Italic();
                            }

                            Tekst(rad.Nummer, fet: true);
                            Tekst(rad.DorTil);
                            Tekst(rad.Dornr);
                            Tekst(rad.Sylindertype, gra: true);
                            Tekst("1", midt: true);

                            for (var k = 0; k < nokler.Count; k++)
                            {
                                var celle = Tone(table.Cell(), k).BorderBottom(grenseTykk).BorderColor(grense).PaddingVertical(2.5f).AlignCenter();
                                if (Apner(nokler[k], rad))
                                {
                                    celle.Text("X").Bold().FontColor(org.Aksent);
                                }
                            }
                        }
                    });
                }

                page.Footer().BorderTop(0.5f).BorderColor(rammeFarge).PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(org.Bunntekst).FontSize(7).FontColor(svak);
                    row.AutoItem().Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(7).FontColor(svak));
                        x.Span($"Låsplan · {prosjekt.Navn} · side ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
