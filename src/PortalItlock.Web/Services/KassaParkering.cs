using System.Collections.Concurrent;

namespace PortalItlock.Web.Services;

// Parkerte salg i kassa: handlekurven legges til side mens neste kunde
// betjenes, og hentes fram igjen senere. Holdes i minnet per organisasjon
// og bruker (et parkert salg er ikke et salg ennå, så ingenting bokføres).
public sealed class KassaParkering
{
    public sealed record Linje(int? VareId, string Navn, decimal Pris, decimal MvaSats, string? Kategori, int Antall);

    public sealed record Parkert(Guid Id, DateTime Tidspunkt, string? Kunde, decimal Rabatt, IReadOnlyList<Linje> Linjer)
    {
        public decimal Sum => Linjer.Sum(l => l.Pris * l.Antall);
    }

    private readonly ConcurrentDictionary<string, List<Parkert>> _parkert = new();

    private static string Nokkel(int? tenantId, int? brukerId) => $"{tenantId}:{brukerId}";

    public IReadOnlyList<Parkert> Hent(int? tenantId, int? brukerId)
    {
        var liste = _parkert.GetOrAdd(Nokkel(tenantId, brukerId), _ => []);
        lock (liste)
        {
            return liste.OrderByDescending(p => p.Tidspunkt).ToList();
        }
    }

    public void Parker(int? tenantId, int? brukerId, Parkert salg)
    {
        var liste = _parkert.GetOrAdd(Nokkel(tenantId, brukerId), _ => []);
        lock (liste)
        {
            liste.Add(salg);
        }
    }

    public Parkert? TaUt(int? tenantId, int? brukerId, Guid id)
    {
        var liste = _parkert.GetOrAdd(Nokkel(tenantId, brukerId), _ => []);
        lock (liste)
        {
            var p = liste.FirstOrDefault(x => x.Id == id);
            if (p is not null)
            {
                liste.Remove(p);
            }

            return p;
        }
    }
}
