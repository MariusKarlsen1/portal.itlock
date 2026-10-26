using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using static PortalItlock.Web.Services.EpostMal;

namespace PortalItlock.Web.Services;

/// <summary>
/// Låsplanen som én matrise (rader = sylindere, kolonner = nøkler), og
/// godkjenningen: sendes til kunden med en lenke, kunden kan endre kryss og
/// godkjenner med navn og signatur. Brukes av Lasplan.razor (internt) og av
/// de anonyme endepunktene /lasplan/g/{token} (kundens side).
/// </summary>
public class LasplanGodkjenningService(ApplicationDbContext db, EmailService epost, ITenantContext tenantContext, ILogger<LasplanGodkjenningService> logger)
{
    public const int GyldigDager = 30;

    public sealed record Nokkelinfo(int Id, string Merking, string Navn, int Antall);

    /// Én rad per sylinder på en dør (DorKomponent) eller reservesylinder. Nokkel er
    /// "d{dorId}-{componentId}" eller "r{reserveId}" - samme som i kryss-nøklene.
    public sealed record Rad(string Nokkel, string Nr, string? DorTil, string? Dornr, string? Etasje,
        string Sylinder, int Antall, bool Montert, int? DorId, int? PlantegningId, double? PosX, double? PosY);

    public sealed record Matrise(List<Nokkelinfo> Nokler, List<Rad> Rader, HashSet<string> Kryss)
    {
        public static string KryssNokkel(int nokkelId, string rad) => $"{nokkelId}|{rad}";
    }

    public sealed record Endring(int NokkelId, string Rad, bool Pa);

    public sealed record LoggPost(DateTime Tid, string Tekst);

    public async Task<Matrise> HentMatriseAsync(int prosjektId)
    {
        var nokler = await db.Nokler.AsNoTracking()
            .Where(n => n.ProsjektId == prosjektId)
            .OrderBy(n => n.Rekkefolge)
            .ToListAsync();

        var dorKomponenter = await db.DorKomponenter.AsNoTracking()
            .Where(k => k.Dor!.ProsjektId == prosjektId && k.Component!.ErSylinder)
            .Include(k => k.Dor)
            .Include(k => k.Component)
            .OrderBy(k => k.Dor!.Dornummer)
            .ToListAsync();

        var reserver = await db.LasplanReserver.AsNoTracking()
            .Where(r => r.ProsjektId == prosjektId)
            .Include(r => r.Component)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var rader = new List<Rad>();
        var nr = 0;
        foreach (var dk in dorKomponenter)
        {
            nr++;
            var d = dk.Dor!;
            rader.Add(new Rad($"d{dk.DorId}-{dk.ComponentId}", nr.ToString(), d.DorTil, d.Dornummer, d.Etasje,
                dk.Component!.Navn, dk.Antall, dk.Montert, d.Id, d.PlantegningId, d.PosX, d.PosY));
        }
        foreach (var r in reserver)
        {
            nr++;
            rader.Add(new Rad($"r{r.Id}", nr.ToString(), r.DorTil, string.IsNullOrWhiteSpace(r.Dornr) ? null : r.Dornr, null,
                r.Component!.Navn, r.Antall, false, null, null, null, null));
        }

        var nokkelIder = nokler.Select(n => n.Id).ToList();
        var kryss = new HashSet<string>();
        foreach (var k in await db.NokkelSylindere.AsNoTracking().Where(k => nokkelIder.Contains(k.NokkelId)).ToListAsync())
        {
            kryss.Add(Matrise.KryssNokkel(k.NokkelId, $"d{k.DorId}-{k.ComponentId}"));
        }
        foreach (var k in await db.NokkelLasplanReserver.AsNoTracking().Where(k => nokkelIder.Contains(k.NokkelId)).ToListAsync())
        {
            kryss.Add(Matrise.KryssNokkel(k.NokkelId, $"r{k.LasplanReserveId}"));
        }

        return new Matrise(
            nokler.Select(n => new Nokkelinfo(n.Id, n.Merking ?? "", n.Navn, n.Antall)).ToList(),
            rader,
            kryss);
    }

    public static List<LoggPost> Logg(Prosjekt p)
    {
        if (string.IsNullOrWhiteSpace(p.LasplanLogg))
        {
            return [];
        }
        try
        {
            return JsonSerializer.Deserialize<List<LoggPost>>(p.LasplanLogg) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static List<Endring> KundeEndringer(Prosjekt p)
    {
        if (string.IsNullOrWhiteSpace(p.LasplanKundeEndringer))
        {
            return [];
        }
        try
        {
            return JsonSerializer.Deserialize<List<Endring>>(p.LasplanKundeEndringer) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void Loggfor(Prosjekt p, string tekst)
    {
        var logg = Logg(p);
        logg.Add(new LoggPost(DateTime.Now, tekst));
        p.LasplanLogg = JsonSerializer.Serialize(logg.TakeLast(100));
    }

    public static bool ErGyldigEpost(string? adresse)
    {
        if (string.IsNullOrWhiteSpace(adresse))
        {
            return false;
        }
        try
        {
            var a = new MailAddress(adresse.Trim());
            return a.Address == adresse.Trim() && a.Host.Contains('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// Lager ny lenke, setter status «Sendt til kunde» og sender e-posten.
    /// Returnerer lenken og om e-posten faktisk gikk - lenken kan uansett kopieres og sendes manuelt.
    public async Task<(string Lenke, bool EpostSendt)> SendTilKundeAsync(int prosjektId, string tilEpost, string? melding, string avsenderNavn, string? avsenderEpost, string baseUrl)
    {
        var p = await db.Prosjekter.Include(x => x.Kunde).FirstAsync(x => x.Id == prosjektId);
        var matrise = await HentMatriseAsync(prosjektId);

        p.LasplanStatus = LasplanStatus.SendtTilKunde;
        p.LasplanSendtTil = tilEpost.Trim();
        p.LasplanSendtDato = DateTime.Now;
        p.LasplanToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        p.LasplanTokenUtloper = DateTime.Now.AddDays(GyldigDager);
        p.LasplanGodkjentAv = null;
        p.LasplanGodkjentFirma = null;
        p.LasplanGodkjentDato = null;
        p.LasplanSignatur = null;
        p.LasplanKundeEndringer = null;
        p.LasplanLast = true;
        Loggfor(p, $"Sendt til {p.LasplanSendtTil} av {avsenderNavn}");
        await db.SaveChangesAsync();

        var lenke = $"{baseUrl.TrimEnd('/')}/lasplan/g/{p.LasplanToken}";
        var sendt = false;
        if (tenantContext.Current is { } tenant)
        {
            var intro = string.IsNullOrWhiteSpace(melding)
                ? $"Hei! Her er låsplanen for {E(p.Navn)}. Se over hvilke nøkler som skal åpne hvilke dører, gjør endringer om noe skal være annerledes, og godkjenn."
                : E(melding.Trim()).Replace("\n", "<br>");
            var fakta = new List<(string, string)> { ("Prosjekt", E(p.Navn)) };
            if (!string.IsNullOrWhiteSpace(p.LasplanSystemnr))
            {
                fakta.Add(("Systemnr.", E(p.LasplanSystemnr)));
            }
            fakta.Add(("Sylindere", matrise.Rader.Count.ToString()));
            fakta.Add(("Nøkler", matrise.Nokler.Count.ToString()));
            fakta.Add(("Sendt av", E(avsenderNavn)));

            var html = Lag(tenant, new Innhold
            {
                Farge = Brun,
                Merke = "Låsplan til godkjenning",
                Tittel = $"Låsplan – {p.Navn}",
                IntroHtml = intro,
                Fakta = fakta,
                KnappTekst = "Se og godkjenn låsplanen",
                KnappUrl = lenke,
                Tips = $"Lenken gjelder til {p.LasplanTokenUtloper:dd.MM.yyyy}. Du trenger ikke logge inn.",
                Grunn = $"Du får denne e-posten fordi {E(tenant.PortalVisningsnavn ?? tenant.Navn)} har sendt deg en låsplan til godkjenning."
            });
            try
            {
                sendt = await epost.SendAsync(p.LasplanSendtTil, $"Låsplan til godkjenning: {p.Navn}", html,
                    svarTil: string.IsNullOrWhiteSpace(avsenderEpost) ? null : avsenderEpost, fraOrganisasjonId: tenant.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kunne ikke sende låsplan for prosjekt {Id}.", prosjektId);
            }
        }

        if (!sendt)
        {
            var lagret = await db.Prosjekter.FirstAsync(x => x.Id == prosjektId);
            Loggfor(lagret, "E-posten kunne ikke sendes – lenken må sendes manuelt");
            await db.SaveChangesAsync();
        }

        return (lenke, sendt);
    }

    public async Task PaminnAsync(int prosjektId, string avsenderNavn, string? avsenderEpost, string baseUrl)
    {
        var p = await db.Prosjekter.FirstAsync(x => x.Id == prosjektId);
        if (p.LasplanStatus != LasplanStatus.SendtTilKunde || string.IsNullOrWhiteSpace(p.LasplanSendtTil))
        {
            return;
        }
        await SendTilKundeAsync(prosjektId, p.LasplanSendtTil, $"Påminnelse: låsplanen for {p.Navn} venter fortsatt på godkjenning.", avsenderNavn, avsenderEpost, baseUrl);
    }

    public async Task GodkjennInterntAsync(int prosjektId, string navn)
    {
        var p = await db.Prosjekter.FirstAsync(x => x.Id == prosjektId);
        p.LasplanStatus = LasplanStatus.Godkjent;
        p.LasplanGodkjentAv = navn;
        p.LasplanGodkjentFirma = null;
        p.LasplanGodkjentDato = DateTime.Now;
        p.LasplanSignatur = null;
        p.LasplanKundeEndringer = null;
        p.LasplanToken = null;
        p.LasplanLast = true;
        Loggfor(p, $"Godkjent internt av {navn}");
        await db.SaveChangesAsync();
    }

    public async Task TilbakeTilArbeidAsync(int prosjektId, string navn)
    {
        var p = await db.Prosjekter.FirstAsync(x => x.Id == prosjektId);
        p.LasplanStatus = LasplanStatus.IArbeid;
        p.LasplanToken = null;
        p.LasplanTokenUtloper = null;
        p.LasplanKundeEndringer = null;
        Loggfor(p, $"Satt tilbake til «I arbeid» av {navn}");
        await db.SaveChangesAsync();
    }

    public async Task<Prosjekt?> FinnForTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 48 || !token.All(Uri.IsHexDigit))
        {
            return null;
        }
        return await db.Prosjekter.Include(p => p.Kunde).FirstOrDefaultAsync(p => p.LasplanToken == token);
    }

    public static bool KanGodkjennes(Prosjekt p) =>
        p.LasplanStatus == LasplanStatus.SendtTilKunde && p.LasplanTokenUtloper is { } u && u > DateTime.Now;

    /// Kunden godkjenner. Kryss er alle kryss kunden vil ha («nøkkelId|rad»); de
    /// lagres, og forskjellen fra før lagres som kundens endringer.
    public async Task<string?> GodkjennFraKundeAsync(string token, string navn, string? firma, string signatur, IReadOnlyCollection<string> kryss, string baseUrl)
    {
        var p = await FinnForTokenAsync(token);
        if (p is null || !KanGodkjennes(p))
        {
            return "Lenken er ikke lenger gyldig. Ta kontakt med den som sendte låsplanen.";
        }
        navn = navn.Trim();
        if (navn.Length is < 2 or > 120)
        {
            return "Skriv inn fullt navn.";
        }
        if (!signatur.StartsWith("data:image/png;base64,", StringComparison.Ordinal) || signatur.Length > 400_000)
        {
            return "Signaturen mangler eller er for stor.";
        }

        var matrise = await HentMatriseAsync(p.Id);
        var gyldige = matrise.Nokler
            .SelectMany(n => matrise.Rader.Select(r => Matrise.KryssNokkel(n.Id, r.Nokkel)))
            .ToHashSet();
        var nye = kryss.Where(gyldige.Contains).ToHashSet();

        var endringer = new List<Endring>();
        foreach (var k in gyldige)
        {
            var for_ = matrise.Kryss.Contains(k);
            var etter = nye.Contains(k);
            if (for_ == etter)
            {
                continue;
            }
            var deler = k.Split('|', 2);
            var nokkelId = int.Parse(deler[0]);
            var rad = deler[1];
            endringer.Add(new Endring(nokkelId, rad, etter));

            if (rad.StartsWith('d'))
            {
                var ider = rad[1..].Split('-');
                int dorId = int.Parse(ider[0]), componentId = int.Parse(ider[1]);
                if (etter)
                {
                    db.NokkelSylindere.Add(new NokkelSylinder { NokkelId = nokkelId, DorId = dorId, ComponentId = componentId });
                }
                else
                {
                    var finnes = await db.NokkelSylindere.FirstOrDefaultAsync(x => x.NokkelId == nokkelId && x.DorId == dorId && x.ComponentId == componentId);
                    if (finnes is not null)
                    {
                        db.NokkelSylindere.Remove(finnes);
                    }
                }
            }
            else
            {
                var reserveId = int.Parse(rad[1..]);
                if (etter)
                {
                    db.NokkelLasplanReserver.Add(new NokkelLasplanReserve { NokkelId = nokkelId, LasplanReserveId = reserveId });
                }
                else
                {
                    var finnes = await db.NokkelLasplanReserver.FirstOrDefaultAsync(x => x.NokkelId == nokkelId && x.LasplanReserveId == reserveId);
                    if (finnes is not null)
                    {
                        db.NokkelLasplanReserver.Remove(finnes);
                    }
                }
            }
        }

        p.LasplanStatus = LasplanStatus.Godkjent;
        p.LasplanGodkjentAv = navn;
        p.LasplanGodkjentFirma = string.IsNullOrWhiteSpace(firma) ? null : firma.Trim()[..Math.Min(firma.Trim().Length, 120)];
        p.LasplanGodkjentDato = DateTime.Now;
        p.LasplanSignatur = signatur;
        p.LasplanKundeEndringer = endringer.Count == 0 ? null : JsonSerializer.Serialize(endringer);
        p.LasplanLast = true;
        Loggfor(p, $"Godkjent og signert av {navn}{(p.LasplanGodkjentFirma is null ? "" : $", {p.LasplanGodkjentFirma}")}"
            + (endringer.Count == 0 ? "" : $" ({endringer.Count} kryss endret)"));
        await db.SaveChangesAsync();

        await VarsleGodkjentAsync(p, endringer.Count, baseUrl);
        return null;
    }

    // Prosjektlederen får beskjed - eller administratorene om prosjektet ikke har en.
    private async Task VarsleGodkjentAsync(Prosjekt p, int antallEndringer, string baseUrl)
    {
        if (tenantContext.Current is not { } tenant)
        {
            return;
        }
        try
        {
            var aktive = await db.Brukere.AsNoTracking()
                .Where(b => b.Aktiv && b.SlettetDato == null && b.Epost != "")
                .ToListAsync();
            var mottakere = p.ProsjektlederId is { } pl && aktive.FirstOrDefault(b => b.Id == pl) is { } leder
                ? [leder]
                : aktive.Where(b => b.Rolle == BrukerRolle.Admin).ToList();

            var fakta = new List<(string, string)>
            {
                ("Prosjekt", E(p.Navn)),
                ("Godkjent av", E(p.LasplanGodkjentAv) + (p.LasplanGodkjentFirma is null ? "" : $", {E(p.LasplanGodkjentFirma)}")),
                ("Når", E(Dato(p.LasplanGodkjentDato ?? DateTime.Now))),
                ("Endringer", antallEndringer == 0 ? "Ingen – godkjent som sendt" : $"{antallEndringer} kryss endret (merket gult i låsplanen)")
            };
            foreach (var b in mottakere)
            {
                var html = Lag(tenant, new Innhold
                {
                    Farge = Gronn,
                    Merke = "Låsplan godkjent",
                    Tittel = $"Låsplanen for {p.Navn} er godkjent",
                    IntroHtml = $"{E(p.LasplanGodkjentAv)} har sett over og signert låsplanen.",
                    Fakta = fakta,
                    KnappTekst = "Åpne låsplanen",
                    KnappUrl = $"{baseUrl.TrimEnd('/')}/prosjekt/{p.Id}/lasplan",
                    Grunn = $"Du får denne e-posten fordi du er prosjektleder eller administrator hos {E(tenant.PortalVisningsnavn ?? tenant.Navn)}."
                });
                await epost.SendAsync(b.Epost, $"Låsplan godkjent: {p.Navn}", html, fraOrganisasjonId: tenant.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Varsel om godkjent låsplan for prosjekt {Id} feilet.", p.Id);
        }
    }
}
