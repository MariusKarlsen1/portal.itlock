using System.Collections.Concurrent;

namespace PortalItlock.Web.Services;

public sealed class PresenceService
{
    private sealed record Okt(int BrukerId, string Navn, DateTime SistSett);

    private readonly ConcurrentDictionary<Guid, Okt> _okter = new();

    public void Registrer(Guid oktId, int brukerId, string navn) =>
        _okter[oktId] = new Okt(brukerId, navn, DateTime.UtcNow);

    public void Fjern(Guid oktId) => _okter.TryRemove(oktId, out _);

    public List<(int BrukerId, string Navn)> HentAktiveBrukere() =>
        _okter.Values
            .GroupBy(o => o.BrukerId)
            .Select(g => (g.Key, g.First().Navn))
            .ToList();
}
