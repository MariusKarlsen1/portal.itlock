using System.Net;

namespace PortalItlock.Web.Services;

// Tripletex tillater 100 kall per 10 sekunder per ansattnøkkel og integrasjon, og
// svarer 429 når grensen er nådd (og etter mange feilede kall på rad). Da venter
// vi så lenge Tripletex ber om (Retry-After eller X-Rate-Limit-Reset, i sekunder)
// og prøver igjen, inntil tre ganger. Ventetiden er maks 6 sekunder per forsøk, så alt holder seg innenfor tidsgrensen på 20 sekunder.
public sealed class TripletexBegrensningHandler : DelegatingHandler
{
    private const int MaksForsok = 3;
    private static readonly TimeSpan MaksVent = TimeSpan.FromSeconds(6);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Innholdet leses inn på forhånd, så det kan sendes på nytt.
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync();
        }

        for (var forsok = 1; ; forsok++)
        {
            var resp = await base.SendAsync(request, ct);
            if (resp.StatusCode != HttpStatusCode.TooManyRequests || forsok >= MaksForsok)
            {
                return resp;
            }

            var vent = Ventetid(resp, forsok);
            resp.Dispose();
            await Task.Delay(vent, ct);
        }
    }

    public static TimeSpan Ventetid(HttpResponseMessage resp, int forsok)
    {
        TimeSpan? fraSvar = null;
        if (resp.Headers.RetryAfter?.Delta is { } delta)
        {
            fraSvar = delta;
        }
        else if (resp.Headers.RetryAfter?.Date is { } dato)
        {
            fraSvar = dato - DateTimeOffset.UtcNow;
        }
        else if (resp.Headers.TryGetValues("X-Rate-Limit-Reset", out var verdier)
                 && int.TryParse(verdier.FirstOrDefault(), out var sek))
        {
            fraSvar = TimeSpan.FromSeconds(sek);
        }

        var vent = fraSvar ?? TimeSpan.FromSeconds(2 * forsok);
        if (vent < TimeSpan.FromSeconds(1))
        {
            vent = TimeSpan.FromSeconds(1);
        }
        return vent > MaksVent ? MaksVent : vent;
    }
}
