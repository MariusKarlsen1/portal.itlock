using System.Collections.Concurrent;

namespace PortalItlock.Web.Services;

[Flags]
public enum TripletexEndring
{
    Ingen = 0,
    Kunder = 1,
    Varer = 2,
    Fakturaer = 4,
    Alt = Kunder | Varer | Fakturaer
}

// Webhooks fra Tripletex (POST /tripletex/webhook/{organisasjon}) legges her,
// og bakgrunnsjobben synker organisasjonen når det har vært stille i 30 sekunder.
// Mange endringer på rad (f.eks. import av kunder i Tripletex) gir da én synk.
// Køen ligger i minnet; går noe tapt ved omstart, tar den nattlige synken det.
public static class TripletexWebhookKo
{
    private static readonly ConcurrentDictionary<int, (TripletexEndring Endring, DateTime Sist)> Ko = new();

    public static TripletexEndring FraHendelse(string? hendelse) => hendelse?.Split('.')[0] switch
    {
        "customer" or "contact" => TripletexEndring.Kunder,
        "product" => TripletexEndring.Varer,
        "invoice" or "closegroup" or "order" => TripletexEndring.Fakturaer,
        _ => TripletexEndring.Alt
    };

    public static void Meld(int organisasjonId, TripletexEndring endring) =>
        Ko.AddOrUpdate(organisasjonId, (endring, DateTime.UtcNow), (_, gammel) => (gammel.Endring | endring, DateTime.UtcNow));

    public static IReadOnlyList<int> Venter() => Ko.Keys.ToList();

    // Endringene for organisasjonen, hvis det har vært stille lenge nok. Tas ut av køen.
    public static TripletexEndring Ta(int organisasjonId, TimeSpan ro)
    {
        if (Ko.TryGetValue(organisasjonId, out var verdi)
            && DateTime.UtcNow - verdi.Sist >= ro
            && Ko.TryRemove(new KeyValuePair<int, (TripletexEndring, DateTime)>(organisasjonId, verdi)))
        {
            return verdi.Endring;
        }
        return TripletexEndring.Ingen;
    }
}
