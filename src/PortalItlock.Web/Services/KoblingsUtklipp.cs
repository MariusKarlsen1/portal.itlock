using System.Collections.Concurrent;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Utklippstavle for koblingsskjema: det som er markert og kopiert (komponenter,
// kabler, streker og tekst) kan limes inn på et annet skjema. Holdes i minnet
// per organisasjon og bruker, så kopien følger brukeren mellom skjema og faner.
public sealed class KoblingsUtklipp
{
    public sealed record Innhold(int FraSkjemaId, IReadOnlyList<KoblingsSymbol> Symboler, IReadOnlyList<KoblingsStrek> Streker)
    {
        public int Antall => Symboler.Count + Streker.Count;
    }

    private readonly ConcurrentDictionary<string, Innhold> _innhold = new();

    public static string Nokkel(int? tenantId, string? bruker) => $"{tenantId}:{bruker}";

    public void Sett(string nokkel, Innhold innhold) => _innhold[nokkel] = innhold;

    public Innhold? Hent(string nokkel) => _innhold.TryGetValue(nokkel, out var i) ? i : null;
}
