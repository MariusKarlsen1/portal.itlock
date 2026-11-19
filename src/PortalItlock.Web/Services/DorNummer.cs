using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;

namespace PortalItlock.Web.Services;

// Dørnummer er unikt per prosjekt: to dører i samme prosjekt kan ikke hete
// det samme (store/små bokstaver og mellomrom i endene regnes som likt).
public static class DorNummer
{
    public static string Norm(string? nr) => (nr ?? "").Trim();

    public static bool Likt(string? a, string? b) => string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);

    public static async Task<List<string>> BrukteAsync(ApplicationDbContext db, int prosjektId, int? unntakDorId = null) =>
        await db.Dorer.AsNoTracking()
            .Where(d => d.ProsjektId == prosjektId && (unntakDorId == null || d.Id != unntakDorId))
            .Select(d => d.Dornummer)
            .ToListAsync();

    public static async Task<bool> FinnesAsync(ApplicationDbContext db, int prosjektId, string nr, int? unntakDorId = null) =>
        (await BrukteAsync(db, prosjektId, unntakDorId)).Any(b => Likt(b, nr));

    // Første ledige variant: «Ny dør», «Ny dør 2», «Ny dør 3» …
    public static string Ledig(IEnumerable<string> brukte, string onsket)
    {
        var sett = brukte.Select(b => Norm(b).ToUpperInvariant()).ToHashSet();
        var grunn = Norm(onsket);
        if (!sett.Contains(grunn.ToUpperInvariant()))
        {
            return grunn;
        }
        for (var i = 2; ; i++)
        {
            var kandidat = $"{grunn} {i}";
            if (!sett.Contains(kandidat.ToUpperInvariant()))
            {
                return kandidat;
            }
        }
    }

    public static async Task<string> LedigAsync(ApplicationDbContext db, int prosjektId, string onsket) =>
        Ledig(await BrukteAsync(db, prosjektId), onsket);

    public static string Melding(string nr) => $"Dør «{Norm(nr)}» finnes allerede i prosjektet. Velg et annet dørnummer.";
}
