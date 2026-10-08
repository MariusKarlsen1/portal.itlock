using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Regresjonstest for cache-fiksen i TripletexService.HentFakturaerAsync
// (CODE_REVIEW.md, SalgPerKunde.razor/Fakturaoversikt.razor): et andre kall
// med samme periode/kunde innen cache-vinduet skal IKKE gjøre noen nye
// HTTP-kall mot Tripletex, og skal returnere nøyaktig samme data.
public class TripletexServiceFakturaCacheTests
{
    private sealed class TellendeHandler : HttpMessageHandler
    {
        public int AntallKall { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AntallKall++;

            var sti = request.RequestUri!.AbsolutePath;
            string json;
            if (sti.Contains("token/session"))
            {
                json = """{"value":{"token":"fake-session-token"}}""";
            }
            else
            {
                json = """{"values":[{"id":1,"invoiceNumber":1001,"invoiceDate":"2026-01-15","invoiceDueDate":"2026-02-15","customer":{"id":5,"name":"Test Kunde"},"amount":1000,"amountOutstanding":0,"isCreditNote":false,"orders":[]}]}""";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task HentFakturaerAsync_gjor_ikke_nytt_HTTP_kall_for_samme_periode_innen_cache_vinduet()
    {
        var handler = new TellendeHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api-test.tripletex.tech/v2/") };
        var options = Options.Create(new TripletexOptions { ConsumerToken = "c", EmployeeToken = "e" });
        var service = new TripletexService(http, options);

        var fraDato = new DateTime(2026, 1, 1);
        var tilDato = new DateTime(2026, 1, 31);

        var (forsteFakturaer, forsteFeil) = await service.HentFakturaerAsync(fraDato, tilDato);
        Assert.Null(forsteFeil);
        Assert.Single(forsteFakturaer);
        var antallKallEtterForste = handler.AntallKall;
        Assert.True(antallKallEtterForste >= 2); // session-token + faktura-kall

        var (andreFakturaer, andreFeil) = await service.HentFakturaerAsync(fraDato, tilDato);

        Assert.Equal(antallKallEtterForste, handler.AntallKall); // ingen nye HTTP-kall
        Assert.Null(andreFeil);
        Assert.Single(andreFakturaer);
        Assert.Equal(forsteFakturaer[0].Nummer, andreFakturaer[0].Nummer);
    }

    [Fact]
    public async Task HentFakturaerAsync_bruker_separat_cache_per_kundeId()
    {
        var handler = new TellendeHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api-test.tripletex.tech/v2/") };
        var options = Options.Create(new TripletexOptions { ConsumerToken = "c", EmployeeToken = "e" });
        var service = new TripletexService(http, options);

        var fraDato = new DateTime(2026, 1, 1);
        var tilDato = new DateTime(2026, 1, 31);

        await service.HentFakturaerAsync(fraDato, tilDato, kundeId: 5);
        var antallEtterForste = handler.AntallKall;

        // Annen kundeId = annen cache-nøkkel = MÅ gjøre et nytt faktura-kall
        // (session-tokenet er fortsatt gyldig, så kun ett nytt kall forventes).
        await service.HentFakturaerAsync(fraDato, tilDato, kundeId: 99);

        Assert.True(handler.AntallKall > antallEtterForste);
    }
}
