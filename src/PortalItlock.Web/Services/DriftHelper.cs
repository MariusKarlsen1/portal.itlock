using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Felles logikk for driftsportalen (/drift): hvilke bygg vaktmesteren har
// tilgang til, og hvor langt en driftsmelding har kommet. Statusen lagres
// ikke på selve meldingen - den leses av det som allerede finnes: om en
// ansatt har åpnet den, og ticketen (med arbeidsordre) som er laget fra den.
public static class DriftHelper
{
    public sealed record Kategori(string Navn, string Ikon);

    public static readonly Kategori[] Kategorier =
    [
        new("Går ikke i lås", "lock"),
        new("Nøkkel / brikke", "komp-dorkort"),
        new("Dørlukker / pumpe", "komp-dorpumpe"),
        new("Automatikk", "komp-dorautomatikk"),
        new("Skadet / ødelagt", "tool"),
        new("Annet", "dokument-linjer")
    ];

    public static string KategoriIkon(string? kategori) =>
        Kategorier.FirstOrDefault(k => k.Navn == kategori)?.Ikon ?? "bell";

    public enum Steg { Sendt, Lest, UnderArbeid, Lost }

    public sealed class MeldingStatus
    {
        public required Driftsmelding Melding { get; init; }
        public Ticket? Ticket { get; init; }

        public Steg Steg => Ticket?.Status switch
        {
            TicketStatus.VenterGodkjenning or TicketStatus.Lukket => Steg.Lost,
            TicketStatus.UnderBehandling => Steg.UnderArbeid,
            _ when Ticket?.Arbeidsordre?.PlanlagtDato is not null => Steg.UnderArbeid,
            _ when Melding.LestAvAnsatt => Steg.Lest,
            _ => Steg.Sendt
        };

        public bool ErApen => Steg != Steg.Lost;

        public string Navn => Steg switch
        {
            Steg.Sendt => "Sendt",
            Steg.Lest => "Mottatt",
            Steg.UnderArbeid => Planlagt is not null ? "Montør planlagt" : "Under arbeid",
            _ => "Løst"
        };

        // Fargeklasse brukt av driftsportalens CSS (dp-t-*).
        public string Farge => Steg switch
        {
            Steg.Sendt => "rod",
            Steg.Lest => "bla",
            Steg.UnderArbeid => "gul",
            _ => "gronn"
        };

        public string Ikon => Steg switch
        {
            Steg.Lost => "check",
            Steg.UnderArbeid => "tool",
            Steg.Lest => "clock",
            _ => "send"
        };

        public DateTime? Planlagt => Ticket?.Arbeidsordre?.PlanlagtDato;
        public string? Montor => Ticket?.Arbeidsordre?.AnsvarligMontor?.Navn ?? Ticket?.AnsvarligBruker?.Navn;
        public DateTime? LostDato => Steg == Steg.Lost ? Ticket?.LukketDato ?? Ticket?.Arbeidsordre?.FullfortDato : null;

        // Kort linje som forteller vaktmesteren hva som skjer nå.
        public string Undertekst => Steg switch
        {
            Steg.Sendt => "Venter på at vi åpner den",
            Steg.Lest => "Vi har sett meldingen og planlegger",
            Steg.UnderArbeid when Planlagt is { } p => $"{(Montor is null ? "Montør" : Fornavn(Montor))} kommer {Dag(p)}",
            Steg.UnderArbeid => "Vi jobber med saken",
            _ => LostDato is { } d ? $"Løst {Dag(d)}" : "Løst"
        };
    }

    public static string Fornavn(string navn) => navn.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? navn;

    public static string Dag(DateTime t)
    {
        var dager = (t.Date - DateTime.Today).Days;
        var dag = dager switch
        {
            0 => "i dag",
            1 => "i morgen",
            -1 => "i går",
            _ => t.ToString("dd.MM")
        };
        return t.TimeOfDay == TimeSpan.Zero ? dag : $"{dag} kl. {t:HH:mm}";
    }

    public static string Siden(DateTime t)
    {
        var d = DateTime.Now - t;
        if (d.TotalMinutes < 1) return "nå";
        if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes} min siden";
        if (t.Date == DateTime.Today) return $"i dag {t:HH:mm}";
        if (t.Date == DateTime.Today.AddDays(-1)) return $"i går {t:HH:mm}";
        return t.ToString("dd.MM.yyyy");
    }

    // Prosjektene (byggene) vaktmesteren er medlem av.
    public static IQueryable<Prosjekt> MineBygg(ApplicationDbContext db, int brukerId) =>
        db.Prosjekter.Where(p => p.Medlemmer.Any(m => m.Id == brukerId));

    public static Task<bool> HarTilgangTilBygg(ApplicationDbContext db, int brukerId, int prosjektId) =>
        db.Prosjekter.Where(p => p.Id == prosjektId).AnyAsync(p => p.Medlemmer.Any(m => m.Id == brukerId));

    // Henter driftsmeldinger med ticket/arbeidsordre slik at statusen kan regnes ut.
    public static async Task<List<MeldingStatus>> HentMeldinger(ApplicationDbContext db, IQueryable<Driftsmelding> utvalg)
    {
        var meldinger = await utvalg
            .Include(m => m.Dor).ThenInclude(d => d!.Prosjekt)
            .Include(m => m.InnmeldtAvBruker)
            .AsNoTracking()
            .OrderByDescending(m => m.OpprettetDato)
            .ToListAsync();

        var ider = meldinger.Select(m => m.Id).ToList();
        var tickets = await db.Tickets
            .Include(t => t.Arbeidsordre).ThenInclude(a => a!.AnsvarligMontor)
            .Include(t => t.AnsvarligBruker)
            .AsNoTracking()
            .Where(t => t.DriftsmeldingId != null && ider.Contains(t.DriftsmeldingId.Value))
            .ToListAsync();

        return meldinger
            .Select(m => new MeldingStatus { Melding = m, Ticket = tickets.FirstOrDefault(t => t.DriftsmeldingId == m.Id) })
            .ToList();
    }
}
