using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// All endring av lagerbeholdning går gjennom denne tjenesten. Beholdningen er
// summen av LagerBevegelser - ingenting skrives over. Etter hver endring
// oppdateres Component.Lagerantall (sum over alle lagre) og Snittkost, slik at
// resten av portalen (varsel om lav beholdning, komponentregisteret) ser
// riktige tall uten å kjenne til bevegelsene.
public class LagerService(ApplicationDbContext db)
{
    public sealed record Beholdning(int ComponentId, int LagerId, int Antall);

    // Sørger for at det finnes et hovedlager (første gang modulen brukes).
    public async Task<Lager> HovedlagerAsync()
    {
        var hoved = await db.Lagre.OrderBy(l => l.Id).FirstOrDefaultAsync(l => l.Type == LagerType.Hovedlager && l.Aktiv);
        if (hoved is not null)
        {
            return hoved;
        }

        hoved = new Lager { Navn = "Hovedlager", Type = LagerType.Hovedlager };
        db.Lagre.Add(hoved);
        await db.SaveChangesAsync();
        return hoved;
    }

    // Beholdning per vare og lager, regnet ut fra bevegelsene.
    public async Task<List<Beholdning>> BeholdningAsync(int? componentId = null, int? lagerId = null)
    {
        var q = db.LagerBevegelser.AsNoTracking().AsQueryable();
        if (componentId is not null)
        {
            q = q.Where(b => b.ComponentId == componentId);
        }

        if (lagerId is not null)
        {
            q = q.Where(b => b.LagerId == lagerId);
        }

        return await q
            .GroupBy(b => new { b.ComponentId, b.LagerId })
            .Select(g => new Beholdning(g.Key.ComponentId, g.Key.LagerId, g.Sum(b => b.Antall)))
            .ToListAsync();
    }

    public async Task<int> AntallAsync(int componentId, int lagerId) =>
        await db.LagerBevegelser.Where(b => b.ComponentId == componentId && b.LagerId == lagerId).SumAsync(b => (int?)b.Antall) ?? 0;

    public static decimal KostFor(Component c) => c.Snittkost ?? c.PrisNetto ?? 0m;

    // Legger til eller trekker fra på ett lager. Positivt antall = inn.
    // kostPerStk brukes ved tilgang (oppdaterer snittkost); ved uttak brukes
    // varens snittkost.
    public async Task RegistrerAsync(int componentId, int lagerId, int antall, LagerBevegelseType type,
        string? notat, int? brukerId, string? brukerNavn, decimal? kostPerStk = null, int? varetellingId = null)
    {
        if (antall == 0)
        {
            return;
        }

        var vare = await db.Components.FirstAsync(c => c.Id == componentId);
        var kost = KostFor(vare);

        if (antall > 0 && kostPerStk is { } nyKost && nyKost > 0)
        {
            // Veid snittkost: (gammel beholdning × snittkost + nye × innpris) / ny beholdning.
            var totalFor = await db.LagerBevegelser.Where(b => b.ComponentId == componentId).SumAsync(b => (int?)b.Antall) ?? 0;
            var grunnlag = Math.Max(0, totalFor);
            vare.Snittkost = grunnlag + antall > 0
                ? Math.Round((grunnlag * kost + antall * nyKost) / (grunnlag + antall), 2)
                : nyKost;
            kost = nyKost;
        }

        db.LagerBevegelser.Add(new LagerBevegelse
        {
            ComponentId = componentId,
            LagerId = lagerId,
            Antall = antall,
            Type = type,
            KostPerStk = kost,
            Notat = string.IsNullOrWhiteSpace(notat) ? null : notat.Trim(),
            BrukerId = brukerId,
            BrukerNavn = brukerNavn,
            VaretellingId = varetellingId
        });

        await db.SaveChangesAsync();
        await OppdaterLagerantallAsync(componentId);
    }

    // Flytter mellom to lagre som to bevegelser med samme FlyttId.
    public async Task FlyttAsync(int componentId, int fraLagerId, int tilLagerId, int antall, string? notat, int? brukerId, string? brukerNavn)
    {
        if (antall <= 0 || fraLagerId == tilLagerId)
        {
            return;
        }

        var vare = await db.Components.FirstAsync(c => c.Id == componentId);
        var kost = KostFor(vare);
        var flyttId = Guid.NewGuid();
        var tid = DateTime.Now;
        var tekst = string.IsNullOrWhiteSpace(notat) ? null : notat.Trim();

        db.LagerBevegelser.Add(new LagerBevegelse { ComponentId = componentId, LagerId = fraLagerId, Antall = -antall, Type = LagerBevegelseType.FlyttUt, KostPerStk = kost, FlyttId = flyttId, Tidspunkt = tid, Notat = tekst, BrukerId = brukerId, BrukerNavn = brukerNavn });
        db.LagerBevegelser.Add(new LagerBevegelse { ComponentId = componentId, LagerId = tilLagerId, Antall = antall, Type = LagerBevegelseType.FlyttInn, KostPerStk = kost, FlyttId = flyttId, Tidspunkt = tid, Notat = tekst, BrukerId = brukerId, BrukerNavn = brukerNavn });
        await db.SaveChangesAsync();
        await OppdaterLagerantallAsync(componentId);
    }

    // Godkjenner en varetelling: avviket (Talt - Registrert) føres som én
    // Telling-bevegelse per vare.
    public async Task GodkjennTellingAsync(int varetellingId, int? brukerId, string? brukerNavn)
    {
        var telling = await db.Varetellinger.Include(t => t.Linjer).FirstAsync(t => t.Id == varetellingId);
        if (telling.Status != VaretellingStatus.Pagar)
        {
            return;
        }

        foreach (var linje in telling.Linjer.Where(l => l.Talt is not null && l.Talt != l.Registrert))
        {
            await RegistrerAsync(linje.ComponentId, telling.LagerId, linje.Talt!.Value - linje.Registrert,
                LagerBevegelseType.Telling, $"Varetelling #{telling.Id}", brukerId, brukerNavn, varetellingId: telling.Id);
        }

        telling.Status = VaretellingStatus.Godkjent;
        telling.Godkjent = DateTime.Now;
        telling.GodkjentAv = brukerNavn;
        await db.SaveChangesAsync();
    }

    // Holder den gamle Lagerantall-kolonnen i takt med bevegelsene.
    private async Task OppdaterLagerantallAsync(int componentId)
    {
        var sum = await db.LagerBevegelser.Where(b => b.ComponentId == componentId).SumAsync(b => (int?)b.Antall) ?? 0;
        var vare = await db.Components.FirstAsync(c => c.Id == componentId);
        vare.Lagerantall = sum;
        if (!vare.ILagerstyring)
        {
            vare.ILagerstyring = true;
        }

        await db.SaveChangesAsync();
    }
}
