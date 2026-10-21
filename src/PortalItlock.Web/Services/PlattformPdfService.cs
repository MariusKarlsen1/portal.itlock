using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

// PDF-ene adminportalen lager, med Full Kontroll-profil:
// - Tilbud og lisensoversikt for én organisasjon (fra abonnementet)
// - Prislisten med pakker, lisenser og tillegg
// - Lisens- og tjenestevilkår med databehandleravtale
// - Fakturagrunnlag for én periode, satt opp som en vanlig faktura
public class PlattformPdfService(PlatformDbContext db, AbonnementService abonnement, PlattformOppsettService oppsett, IWebHostEnvironment env)
{
    private static readonly CultureInfo Nb = CultureInfo.GetCultureInfo("nb-NO");
    private static readonly Color Aksent = Color.FromHex("#8A5A34");
    private static readonly Color AksentLys = Color.FromHex("#F4ECE2");
    private static readonly Color Ink = Color.FromHex("#1A1712");
    private static readonly Color Dempet = Color.FromHex("#6E675C");
    private static readonly Color Linje = Color.FromHex("#E6DFD4");
    private static readonly Color Rad = Color.FromHex("#FBFAF7");

    private static string Kr(decimal v) => v.ToString("#,##0", Nb) + " kr";
    private static string Kr2(decimal v) => v.ToString("#,##0.00", Nb);

    private byte[]? Logo()
    {
        var fil = Path.Combine(env.WebRootPath, "fullkontroll-logo-mark.png");
        return File.Exists(fil) ? File.ReadAllBytes(fil) : null;
    }

    private static void Side(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(1.9f, Unit.Centimetre);
        page.MarginTop(1.5f, Unit.Centimetre);
        page.MarginBottom(1.3f, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Ink).LineHeight(1.35f));
    }

    private void Hode(IContainer c, PlattformInnstillinger inn, string hoyre)
    {
        c.Row(row =>
        {
            row.RelativeItem().Row(r =>
            {
                if (Logo() is { } logo)
                {
                    r.ConstantItem(30).Height(30).Image(logo).FitArea();
                    r.ConstantItem(8);
                }

                r.RelativeItem().AlignMiddle().Column(k =>
                {
                    k.Item().Text("Full Kontroll").FontSize(15).Bold().FontColor(Ink);
                    k.Item().Text($"levert av {inn.Firmanavn}").FontSize(8).FontColor(Dempet);
                });
            });
            row.ConstantItem(170).AlignRight().AlignMiddle().Text(hoyre).FontSize(8.5f).FontColor(Dempet);
        });
    }

    private static void Bunn(IContainer c, PlattformInnstillinger inn)
    {
        c.PaddingTop(6).BorderTop(0.75f).BorderColor(Linje).PaddingTop(5).Row(row =>
        {
            var deler = new[] { inn.Firmanavn, inn.OrgNr is { Length: > 0 } o ? $"Org.nr. {o}" : null, inn.Epost, inn.Telefon, inn.Nettside }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            row.RelativeItem().Text(string.Join("  ·  ", deler)).FontSize(7.5f).FontColor(Dempet);
            row.ConstantItem(60).AlignRight().Text(t =>
            {
                t.Span("Side ").FontSize(7.5f).FontColor(Dempet);
                t.CurrentPageNumber().FontSize(7.5f).FontColor(Dempet);
                t.Span(" av ").FontSize(7.5f).FontColor(Dempet);
                t.TotalPages().FontSize(7.5f).FontColor(Dempet);
            });
        });
    }

    private static void Overskrift(ColumnDescriptor col, string tekst) =>
        col.Item().PaddingTop(14).PaddingBottom(4).Text(tekst).FontSize(12.5f).Bold().FontColor(Aksent);

    // Tekst med «## » som overskrift, «- » som punkt og tom linje som avsnitt.
    private static void Brodtekst(ColumnDescriptor col, string? tekst, string leverandor)
    {
        foreach (var linje in (tekst ?? "").Replace("{Leverandor}", leverandor).Replace("\r", "").Split('\n'))
        {
            var l = linje.Trim();
            if (l.Length == 0)
            {
                col.Item().Height(4);
            }
            else if (l.StartsWith("## "))
            {
                col.Item().PaddingTop(10).PaddingBottom(2).Text(l[3..]).FontSize(11).Bold().FontColor(Aksent);
            }
            else if (l.StartsWith("- "))
            {
                col.Item().PaddingLeft(10).Row(r =>
                {
                    r.ConstantItem(10).Text("•").FontColor(Aksent);
                    r.RelativeItem().Text(l[2..]);
                });
            }
            else
            {
                col.Item().Text(l);
            }
        }
    }

    private static void TabellHode(TableDescriptor t, params (string Tekst, bool Hoyre)[] kolonner)
    {
        t.Header(h =>
        {
            foreach (var (tekst, hoyre) in kolonner)
            {
                var celle = h.Cell().Background(AksentLys).PaddingVertical(5).PaddingHorizontal(6);
                (hoyre ? celle.AlignRight() : celle).Text(tekst).FontSize(8).Bold().FontColor(Aksent);
            }
        });
    }

    private static IContainer Celle(IContainer c, bool hoyre = false, bool sum = false)
    {
        var x = c.BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(5).PaddingHorizontal(6);
        if (sum)
        {
            x = x.Background(Rad);
        }

        return hoyre ? x.AlignRight() : x;
    }

    // ---------- Tilbud og lisensoversikt ----------

    public async Task<byte[]?> TilbudAsync(int tenantId)
    {
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
        {
            return null;
        }

        var inn = await oppsett.HentAsync();
        var abo = await abonnement.SikreAbonnementAsync(tenant);
        var linjer = (await abonnement.LinjerAsync(tenant.Id)).Where(l => l.Antall > 0).ToList();
        var pakke = abo.Pakke is null ? null : await db.PrisPakker.AsNoTracking().FirstOrDefaultAsync(p => p.Nokkel == abo.Pakke);
        var etablering = abo.Etableringsgebyr ?? (await db.PrisProdukter.AsNoTracking().FirstOrDefaultAsync(p => p.Nokkel == PrisProdukt.Etablering))?.Kr ?? 0;
        var rabatt = AbonnementService.RabattProsent(linjer, inn);
        var rabattBelop = Math.Round(AbonnementService.Rabattgrunnlag(linjer) * rabatt / 100m, 2);
        var perManed = AbonnementService.Maanedsbelop(linjer, inn);
        var arsGratis = abo.Intervall == FakturaIntervall.Ar ? inn.GratisManeder : 0;

        return Document.Create(d => d.Page(page =>
        {
            Side(page);
            page.Header().Element(c => Hode(c, inn, $"Tilbud\n{DateTime.Today:dd.MM.yyyy}"));
            page.Footer().Element(c => Bunn(c, inn));
            page.Content().PaddingTop(18).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(k =>
                    {
                        k.Item().Text(tenant.Navn).Bold();
                        if (!string.IsNullOrWhiteSpace(tenant.Adresse)) k.Item().Text(tenant.Adresse);
                        if (!string.IsNullOrWhiteSpace(tenant.Postnr) || !string.IsNullOrWhiteSpace(tenant.Sted)) k.Item().Text($"{tenant.Postnr} {tenant.Sted}".Trim());
                        if (!string.IsNullOrWhiteSpace(tenant.Kundeansvarlig)) k.Item().Text($"Att: {tenant.Kundeansvarlig}");
                    });
                    row.ConstantItem(200).Column(k =>
                    {
                        k.Item().AlignRight().Text(inn.Firmanavn).Bold();
                        foreach (var s in new[] { inn.Adresse, $"{inn.Postnr} {inn.Sted}".Trim(), inn.OrgNr is { Length: > 0 } o ? $"Org.nr. {o}" : null, inn.Epost, inn.Telefon })
                        {
                            if (!string.IsNullOrWhiteSpace(s)) k.Item().AlignRight().Text(s).FontColor(Dempet);
                        }
                    });
                });

                col.Item().PaddingTop(20).Text($"Tilbud: Full Kontroll for {tenant.Navn}").FontSize(18).Bold();
                col.Item().PaddingTop(2).Text("Prosjekter, tilbud, montering, service og FDV i ett system").FontSize(10).FontColor(Dempet);
                col.Item().PaddingTop(8).Column(k => Brodtekst(k, inn.TilbudIntro, inn.Firmanavn));

                if (pakke is not null)
                {
                    Overskrift(col, $"Pakke: {pakke.Navn}");
                    if (!string.IsNullOrWhiteSpace(pakke.Beskrivelse)) col.Item().Text(pakke.Beskrivelse).FontColor(Dempet);
                    foreach (var punkt in (pakke.Innhold ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        col.Item().PaddingLeft(10).Row(r => { r.ConstantItem(10).Text("•").FontColor(Aksent); r.RelativeItem().Text(punkt.Trim()); });
                    }
                }

                Overskrift(col, "Lisenser og moduler");
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(3.2f); c.RelativeColumn(1); c.RelativeColumn(1.3f); c.RelativeColumn(1.4f); });
                    TabellHode(t, ("Hva", false), ("Antall", true), ("Pris", true), ("Per måned", true));
                    foreach (var l in linjer)
                    {
                        t.Cell().Element(c => Celle(c)).Column(k =>
                        {
                            k.Item().Text(l.Navn).Bold();
                            if (!string.IsNullOrWhiteSpace(l.Beskrivelse)) k.Item().Text(l.Beskrivelse).FontSize(8).FontColor(Dempet);
                        });
                        t.Cell().Element(c => Celle(c, true)).Text(l.PakkeStorrelse > 1 ? $"{l.Antall} ({l.Enheter} pk.)" : l.Antall.ToString(Nb));
                        t.Cell().Element(c => Celle(c, true)).Text(l.PakkeStorrelse > 1 ? $"{Kr(l.Pris)} / {l.PakkeStorrelse}" : Kr(l.Pris));
                        t.Cell().Element(c => Celle(c, true)).Text(Kr(l.Sum));
                    }

                    if (rabatt > 0 && rabattBelop > 0)
                    {
                        t.Cell().ColumnSpan(3).Element(c => Celle(c)).Text($"Volumrabatt {rabatt} % på lisenser");
                        t.Cell().Element(c => Celle(c, true)).Text("−" + Kr(rabattBelop));
                    }

                    t.Cell().ColumnSpan(3).Element(c => Celle(c, false, true)).Text("Sum per måned, eks. mva").Bold();
                    t.Cell().Element(c => Celle(c, true, true)).Text(Kr(perManed)).Bold();
                    t.Cell().ColumnSpan(3).Element(c => Celle(c, false, true)).Text(arsGratis > 0 ? $"Per år med årlig forskudd ({arsGratis} måneder gratis)" : "Per år").Bold();
                    t.Cell().Element(c => Celle(c, true, true)).Text(Kr(perManed * (12 - arsGratis))).Bold();
                });

                if (etablering > 0)
                {
                    Overskrift(col, "Oppstart");
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Etablering: oppsett av portalen, import av kunder og produkter, og opplæring av administratorer, prosjektledere og montører. Faktureres én gang.");
                        r.ConstantItem(110).AlignRight().Text(Kr(etablering)).Bold();
                    });
                }

                Overskrift(col, "Betingelser");
                var punkter = new List<string>
                {
                    "Alle priser er i NOK og uten mva.",
                    $"Abonnementet faktureres {abo.Intervall.Visningsnavn().ToLowerInvariant()}. Betalingsfrist {abo.BetalingsfristDager} dager.",
                    "Nye lisenser faktureres fra de aktiveres. Prisene kan reguleres hvert år etter konsumprisindeksen.",
                };
                if (abo.ProveperiodeTil is { } prove) punkter.Add($"Prøveperiode til {prove:dd.MM.yyyy}. Abonnementet faktureres fra dagen etter.");
                if (abo.BindingTil is { } binding) punkter.Add($"Binding til {binding:dd.MM.yyyy}.");
                punkter.Add($"Lisens- og tjenestevilkår for Full Kontroll versjon {inn.VilkarVersjon}, med databehandleravtale, gjelder.");
                punkter.Add("Tilbudet gjelder i 30 dager.");
                foreach (var p in punkter)
                {
                    col.Item().PaddingLeft(10).Row(r => { r.ConstantItem(10).Text("•").FontColor(Aksent); r.RelativeItem().Text(p); });
                }

                col.Item().PaddingTop(20).Text("Med vennlig hilsen").FontColor(Dempet);
                col.Item().Text(inn.Kontaktperson ?? inn.Firmanavn).Bold();
                col.Item().Text(string.Join("  ·  ", new[] { inn.Telefon, inn.Epost }.Where(s => !string.IsNullOrWhiteSpace(s)))).FontColor(Dempet);
            });
        })).GeneratePdf();
    }

    // ---------- Prisliste ----------

    public async Task<byte[]> PrislisteAsync()
    {
        var inn = await oppsett.HentAsync();
        var produkter = await abonnement.PrislisteAsync();
        var pakker = await db.PrisPakker.AsNoTracking().OrderBy(p => p.Rekkefolge).ToListAsync();
        var pakkePriser = await db.PakkePriser.AsNoTracking().ToListAsync();
        decimal PrisI(string pakke, PrisProdukt p) => pakkePriser.FirstOrDefault(x => x.PakkeNokkel == pakke && x.ProduktNokkel == p.Nokkel)?.Kr ?? p.Kr;
        var lisenser = produkter.Where(p => p.Type == PrisType.Lisens && (p.PakkeStorrelse ?? 1) <= 1).ToList();

        return Document.Create(d => d.Page(page =>
        {
            Side(page);
            page.Header().Element(c => Hode(c, inn, $"Prisliste\nGjelder fra {DateTime.Today:dd.MM.yyyy}"));
            page.Footer().Element(c => Bunn(c, inn));
            page.Content().PaddingTop(18).Column(col =>
            {
                col.Item().Text("Prisliste Full Kontroll").FontSize(18).Bold();
                col.Item().Text("Alle priser er per måned i NOK uten mva, med mindre annet står.").FontColor(Dempet);

                if (pakker.Count > 0)
                {
                    Overskrift(col, "Pakker");
                    col.Item().Row(row =>
                    {
                        foreach (var p in pakker)
                        {
                            row.RelativeItem().Padding(3).Border(0.75f).BorderColor(Linje).Padding(8).Column(k =>
                            {
                                k.Item().Text(p.Navn).FontSize(12).Bold().FontColor(Aksent);
                                if (!string.IsNullOrWhiteSpace(p.Beskrivelse)) k.Item().Text(p.Beskrivelse).FontSize(8).FontColor(Dempet);
                                foreach (var punkt in (p.Innhold ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
                                {
                                    k.Item().PaddingTop(2).Text("• " + punkt.Trim()).FontSize(8.5f);
                                }
                            });
                        }
                    });
                }

                Overskrift(col, "Lisens per bruker");
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        if (pakker.Count == 0) c.RelativeColumn(1.2f);
                        foreach (var _ in pakker) c.RelativeColumn(1.2f);
                    });
                    var kol = new List<(string, bool)> { ("Lisens", false) };
                    kol.AddRange(pakker.Count == 0 ? [("Pris", true)] : pakker.Select(p => (p.Navn, true)));
                    TabellHode(t, [.. kol]);
                    foreach (var l in lisenser)
                    {
                        t.Cell().Element(c => Celle(c)).Column(k =>
                        {
                            k.Item().Text(l.Navn).Bold();
                            if (!string.IsNullOrWhiteSpace(l.Beskrivelse)) k.Item().Text(l.Beskrivelse).FontSize(8).FontColor(Dempet);
                        });
                        if (pakker.Count == 0)
                        {
                            t.Cell().Element(c => Celle(c, true)).Text(Kr(l.Kr));
                        }

                        foreach (var p in pakker)
                        {
                            t.Cell().Element(c => Celle(c, true)).Text(Kr(PrisI(p.Nokkel, l)));
                        }
                    }
                });

                var tillegg = produkter.Where(p => p.Type is PrisType.Modul or PrisType.Fast or PrisType.Engang || (p.PakkeStorrelse ?? 1) > 1).ToList();
                Overskrift(col, "Grunnpakke, driftslisenser og tillegg");
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(3.5f); c.RelativeColumn(1.4f); c.RelativeColumn(1.2f); });
                    TabellHode(t, ("Hva", false), ("Enhet", false), ("Pris", true));
                    foreach (var p in tillegg)
                    {
                        t.Cell().Element(c => Celle(c)).Column(k =>
                        {
                            k.Item().Text(p.Navn).Bold();
                            if (!string.IsNullOrWhiteSpace(p.Beskrivelse)) k.Item().Text(p.Beskrivelse).FontSize(8).FontColor(Dempet);
                        });
                        t.Cell().Element(c => Celle(c)).Text((p.PakkeStorrelse ?? 1) > 1 ? $"pakke à {p.PakkeStorrelse} per mnd" : p.Type.Enhet());
                        t.Cell().Element(c => Celle(c, true)).Text(Kr(p.Kr));
                    }
                });

                Overskrift(col, "Rabatt");
                col.Item().Text(!inn.VolumrabattAktiv ? "Ingen volumrabatt." : $"Volumrabatt på lisenser: {inn.Rabatt1Prosent} % fra {inn.Rabatt1FraAntall} lisenser, {inn.Rabatt2Prosent} % fra {inn.Rabatt2FraAntall} lisenser.");
                if (inn.GratisManeder > 0)
                {
                    col.Item().Text($"Årlig forskudd: {inn.GratisManeder} måneder gratis.");
                }

                col.Item().PaddingTop(12).Text($"Lisens- og tjenestevilkår for Full Kontroll versjon {inn.VilkarVersjon} gjelder. Prisene kan reguleres hvert år etter konsumprisindeksen.").FontSize(8.5f).FontColor(Dempet);
            });
        })).GeneratePdf();
    }

    // ---------- Vilkår og databehandleravtale ----------

    public async Task<byte[]> VilkarAsync()
    {
        var inn = await oppsett.HentAsync();
        return Document.Create(d =>
        {
            d.Page(page =>
            {
                Side(page);
                page.Content().AlignMiddle().Column(col =>
                {
                    if (Logo() is { } logo)
                    {
                        col.Item().AlignCenter().Height(60).Image(logo).FitHeight();
                    }

                    col.Item().PaddingTop(16).AlignCenter().Text("Full Kontroll").FontSize(26).Bold().FontColor(Ink);
                    col.Item().PaddingTop(30).AlignCenter().Text("Lisens- og tjenestevilkår").FontSize(18).Bold();
                    col.Item().AlignCenter().Text("med databehandleravtale (GDPR)").FontSize(13).FontColor(Dempet);
                    col.Item().PaddingTop(30).AlignCenter().Text(inn.Firmanavn).FontSize(10);
                    col.Item().AlignCenter().Text($"Versjon {inn.VilkarVersjon} · gjelder fra {inn.VilkarGjelderFra:dd.MM.yyyy}").FontSize(10).FontColor(Dempet);
                });
            });
            d.Page(page =>
            {
                Side(page);
                page.Header().Element(c => Hode(c, inn, $"Vilkår versjon {inn.VilkarVersjon}"));
                page.Footer().Element(c => Bunn(c, inn));
                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Item().Text("Lisens- og tjenestevilkår for Full Kontroll").FontSize(16).Bold();
                    Brodtekst(col, inn.VilkarTekst ?? Vilkar.StandardVilkar, inn.Firmanavn);
                    col.Item().PageBreak();
                    col.Item().Text("Databehandleravtale (GDPR)").FontSize(16).Bold();
                    Brodtekst(col, inn.DatabehandlerTekst ?? Vilkar.StandardDatabehandleravtale, inn.Firmanavn);
                });
            });
        }).GeneratePdf();
    }

    // ---------- Fakturagrunnlag ----------

    public async Task<byte[]?> FakturaAsync(int fakturaId)
    {
        var f = await db.PlattformFakturaer.AsNoTracking().Include(x => x.Linjer).Include(x => x.Tenant).FirstOrDefaultAsync(x => x.Id == fakturaId);
        return f?.Tenant is { } tenant ? await FakturaAsync(f, tenant, null) : null;
    }

    // Neste faktura slik den blir, før den er lagret som utkast.
    public async Task<byte[]?> NesteFakturaAsync(int tenantId)
    {
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
        {
            return null;
        }

        var f = await abonnement.ForhandsvisNesteAsync(tenant);
        f.Opprettet = DateTime.Now;
        return await FakturaAsync(f, tenant, "FORHÅNDSVISNING");
    }

    private async Task<byte[]> FakturaAsync(PlattformFaktura f, Tenant tenant, string? overskrift)
    {
        var inn = await oppsett.HentAsync();
        var abo = await db.TenantAbonnementer.AsNoTracking().FirstOrDefaultAsync(a => a.TenantId == tenant.Id);
        var dato = f.Fakturadato ?? f.Opprettet.Date;
        var forfall = f.Forfall ?? dato.AddDays(abo?.BetalingsfristDager ?? 14);
        var sumEks = f.Linjer.Sum(l => l.Belop);
        var mva = Math.Round(sumEks * 0.25m, 2);
        var tittel = overskrift ?? (f.TripletexFakturaNummer is null ? "FAKTURAGRUNNLAG" : "FAKTURA");

        return Document.Create(d => d.Page(page =>
        {
            Side(page);
            page.Footer().Element(c => Bunn(c, inn));
            page.Content().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().PaddingTop(30).Column(k =>
                    {
                        k.Item().Text(tenant.Navn);
                        if (!string.IsNullOrWhiteSpace(tenant.Adresse)) k.Item().Text(tenant.Adresse);
                        if (!string.IsNullOrWhiteSpace(tenant.Postnr) || !string.IsNullOrWhiteSpace(tenant.Sted)) k.Item().Text($"{tenant.Postnr} {tenant.Sted}".Trim());
                    });
                    row.ConstantItem(240).Column(k =>
                    {
                        k.Item().Row(r =>
                        {
                            if (Logo() is { } logo)
                            {
                                r.ConstantItem(26).Height(26).Image(logo).FitArea();
                                r.ConstantItem(7);
                            }

                            r.RelativeItem().AlignMiddle().Text("Full Kontroll").FontSize(14).Bold();
                        });
                        k.Item().PaddingTop(10).Text(inn.Firmanavn).Bold();
                        foreach (var (lbl, v) in new[] { ("", inn.Adresse), ("", $"{inn.Postnr} {inn.Sted}".Trim()), ("Org.nr.:", inn.OrgNr), ("Telefon:", inn.Telefon), ("E-post:", inn.Epost) })
                        {
                            if (string.IsNullOrWhiteSpace(v)) continue;
                            k.Item().Row(r => { r.ConstantItem(55).Text(lbl).FontSize(8.5f); r.RelativeItem().Text(v).FontSize(8.5f); });
                        }
                    });
                });

                col.Item().PaddingTop(26).Row(row =>
                {
                    row.RelativeItem();
                    row.ConstantItem(240).Column(k =>
                    {
                        k.Item().Text(tittel).FontSize(16).Bold();
                        void Felt(string l, string v, bool fet = false) => k.Item().Row(r =>
                        {
                            r.RelativeItem().Text(l);
                            var t = r.RelativeItem().AlignRight().Text(v);
                            if (fet) t.Bold();
                        });
                        k.Item().Height(6);
                        Felt("Fakturanr.:", f.TripletexFakturaNummer?.ToString(Nb) ?? "Lages i Tripletex", true);
                        Felt("Fakturadato:", dato.ToString("dd.MM.yyyy"));
                        Felt("Periode:", AbonnementService.PeriodeTekst(f.PeriodeFra, f.PeriodeTil));
                        k.Item().PaddingTop(8).Text("Betalingsinformasjon").Bold();
                        Felt("Forfallsdato:", forfall.ToString("dd.MM.yyyy"));
                        if (!string.IsNullOrWhiteSpace(inn.Kontonummer)) Felt("Kontonummer:", inn.Kontonummer!);
                    });
                });

                col.Item().PaddingTop(20).Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(0.9f); c.RelativeColumn(1.3f); c.RelativeColumn(1.3f); c.RelativeColumn(1.1f); c.RelativeColumn(1.3f); });
                    t.Header(h =>
                    {
                        foreach (var (tekst, hoyre) in new[] { ("Beskrivelse", false), ("Antall", true), ("Enh.pris (eks. mva)", true), ("Beløp (eks. mva)", true), ("Mva (25 %)", true), ("Beløp (inkl. mva)", true) })
                        {
                            var c = h.Cell().BorderBottom(1).BorderColor(Ink).PaddingVertical(4).PaddingHorizontal(4);
                            (hoyre ? c.AlignRight() : c).Text(tekst).FontSize(8).Italic();
                        }
                    });
                    foreach (var l in f.Linjer.OrderBy(x => x.Rekkefolge))
                    {
                        var lmva = Math.Round(l.Belop * 0.25m, 2);
                        t.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingHorizontal(4).Text(l.Tekst);
                        t.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(l.Antall.ToString("0.##", Nb));
                        t.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Kr2(l.Pris));
                        t.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Kr2(l.Belop));
                        t.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Kr2(lmva));
                        t.Cell().BorderBottom(0.5f).BorderColor(Linje).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Kr2(l.Belop + lmva));
                    }

                    var betales = string.IsNullOrWhiteSpace(inn.Kontonummer) ? "Å betale" : $"Betales til bankkonto {inn.Kontonummer}";
                    t.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Ink).PaddingVertical(5).PaddingHorizontal(4).Text(betales).Bold();
                    t.Cell().ColumnSpan(2).BorderBottom(1).BorderColor(Ink).PaddingVertical(5).PaddingHorizontal(4).AlignRight().Text($"NOK {Kr2(sumEks + mva)}").Bold();
                });

                if (f.TripletexFakturaNummer is null)
                {
                    col.Item().PaddingTop(14).Text("Dette er et grunnlag. Selve fakturaen med KID sendes fra regnskapssystemet.").FontSize(8).FontColor(Dempet);
                }
            });
        })).GeneratePdf();
    }
}
