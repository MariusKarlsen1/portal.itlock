using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Kallene mot Tripletex, sjekket mot OpenAPI-spesifikasjonen (v2.75.24):
// lister hentes side for side, ordren sier at prisene er eks. mva, mva-koden slås
// opp, produktets mva-kode brukes, nye kunder får sendemåte, og 429 prøves igjen.
public class TripletexApiTests
{
    private sealed class FalskTripletex(Func<HttpRequestMessage, string?, HttpResponseMessage> svar) : HttpMessageHandler
    {
        public List<(HttpMethod Metode, string Sti, string? Body)> Kall { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Kall.Add((request.Method, request.RequestUri!.PathAndQuery, body));
            return svar(request, body);
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode kode = HttpStatusCode.OK) =>
        new(kode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static TripletexService Lag(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api-test.tripletex.tech/v2/") },
            new TripletexService.Nokler("c-" + Guid.NewGuid(), "e-" + Guid.NewGuid(), true), 1);

    private static string Produkter(int fra, int antall) =>
        "{\"values\":[" + string.Join(",", Enumerable.Range(fra, antall).Select(i => $"{{\"id\":{i},\"number\":\"P{i}\",\"name\":\"Produkt {i}\"}}")) + "]}";

    [Fact]
    public async Task Produkter_hentes_side_for_side_med_from_til_alt_er_hentet()
    {
        var falsk = new FalskTripletex((req, _) =>
        {
            var sti = req.RequestUri!.PathAndQuery;
            if (sti.Contains("token/session")) return Json("""{"value":{"token":"t"}}""");
            return sti.Contains("from=0&") ? Json(Produkter(0, 1000)) : Json(Produkter(1000, 2));
        });

        var (produkter, feil) = await Lag(falsk).HentProdukterAsync();

        Assert.Null(feil);
        Assert.Equal(1002, produkter.Count);
        Assert.Equal(2, falsk.Kall.Count(k => k.Sti.Contains("/product")));
        Assert.Contains(falsk.Kall, k => k.Sti.Contains("from=1000&count=1000"));
    }

    [Fact]
    public async Task Ordre_sender_priser_eks_mva_og_bruker_oppslatt_mva_og_produktets_mva()
    {
        var falsk = new FalskTripletex((req, _) =>
        {
            var sti = req.RequestUri!.PathAndQuery;
            if (sti.Contains("token/session")) return Json("""{"value":{"token":"t"}}""");
            if (sti.Contains("ledger/vatType")) return Json("""{"values":[{"id":99,"number":"3","percentage":25}]}""");
            if (sti.Contains("/product")) return Json("""{"values":[{"id":7,"number":"TA100","name":"Montasje","vatType":{"id":55}}]}""");
            if (sti.EndsWith("/order")) return Json("""{"value":{"id":500,"number":"10255"}}""");
            return Json("""{"values":[]}""");
        });

        var (id, nr, feil) = await Lag(falsk).OpprettOrdreAsync(12, "AO 231",
        [
            new TripletexService.OrdreLinjeInput("Kortleser", 1, 1980),
            new TripletexService.OrdreLinjeInput("Montasje", 3, 1172, "TA100"),
        ]);

        Assert.Null(feil);
        Assert.Equal(500, id);
        Assert.Equal("10255", nr);

        var ordre = JsonDocument.Parse(falsk.Kall.Single(k => k.Sti.EndsWith("/order")).Body!).RootElement;
        Assert.False(ordre.GetProperty("isPrioritizeAmountsIncludingVat").GetBoolean());

        var linjer = JsonDocument.Parse(falsk.Kall.Single(k => k.Sti.Contains("order/orderline/list")).Body!).RootElement;
        Assert.Equal(99, linjer[0].GetProperty("vatType").GetProperty("id").GetInt32());
        Assert.Equal(55, linjer[1].GetProperty("vatType").GetProperty("id").GetInt32());
        Assert.Equal(7, linjer[1].GetProperty("product").GetProperty("id").GetInt32());
        Assert.True(linjer[0].TryGetProperty("unitPriceExcludingVatCurrency", out _));
    }

    [Fact]
    public async Task Ordre_faller_tilbake_til_mva_id_3_nar_oppslaget_feiler()
    {
        var falsk = new FalskTripletex((req, _) =>
        {
            var sti = req.RequestUri!.PathAndQuery;
            if (sti.Contains("token/session")) return Json("""{"value":{"token":"t"}}""");
            if (sti.Contains("ledger/vatType")) return Json("{}", HttpStatusCode.Forbidden);
            if (sti.EndsWith("/order")) return Json("""{"value":{"id":1,"number":"1"}}""");
            return Json("""{"values":[]}""");
        });

        await Lag(falsk).OpprettOrdreAsync(12, null, [new TripletexService.OrdreLinjeInput("Vare", 1, 100)]);

        var linjer = JsonDocument.Parse(falsk.Kall.Single(k => k.Sti.Contains("order/orderline/list")).Body!).RootElement;
        Assert.Equal(3, linjer[0].GetProperty("vatType").GetProperty("id").GetInt32());
    }

    [Theory]
    [InlineData("post@firma.no", null, null, "EMAIL")]
    [InlineData(null, null, null, "MANUAL")]
    [InlineData("ugyldig adresse", "Storgata 1", "4400", null)]
    [InlineData(null, "Storgata 1", "4400", null)]
    public void Ny_kunde_far_sendemate_som_ikke_krever_EHF(string? epost, string? adresse, string? postnr, string? forventet)
    {
        var kunde = new TripletexService.KundeOppdatering("Firma AS", "912345678", epost, null, adresse, postnr, "Flekkefjord");
        Assert.Equal(forventet, TripletexService.SendemateForNyKunde(kunde));
    }

    [Fact]
    public async Task Ny_kunde_sendes_med_invoiceSendMethod_men_oppdatering_endrer_den_ikke()
    {
        var falsk = new FalskTripletex((req, _) =>
            req.RequestUri!.PathAndQuery.Contains("token/session")
                ? Json("""{"value":{"token":"t"}}""")
                : Json("""{"value":{"id":44,"customerNumber":10044}}"""));
        var tx = Lag(falsk);
        var kunde = new TripletexService.KundeOppdatering("Firma AS", "912345678", "post@firma.no", null, null, null, null);

        await tx.OpprettKundeAsync(kunde);
        await tx.OppdaterKundeAsync(44, kunde);

        var ny = JsonDocument.Parse(falsk.Kall.Single(k => k.Metode == HttpMethod.Post && k.Sti.EndsWith("/customer")).Body!).RootElement;
        var oppdatert = JsonDocument.Parse(falsk.Kall.Single(k => k.Metode == HttpMethod.Put).Body!).RootElement;
        Assert.Equal("EMAIL", ny.GetProperty("invoiceSendMethod").GetString());
        Assert.False(oppdatert.TryGetProperty("invoiceSendMethod", out _));
    }

    [Fact]
    public async Task Svar_429_ventes_ut_og_proves_igjen()
    {
        var forsok = 0;
        var falsk = new FalskTripletex((_, _) =>
        {
            forsok++;
            if (forsok == 1)
            {
                var r = Json("{}", HttpStatusCode.TooManyRequests);
                r.Headers.Add("X-Rate-Limit-Reset", "1");
                return r;
            }
            return Json("""{"ok":true}""");
        });
        var http = new HttpClient(new TripletexBegrensningHandler { InnerHandler = falsk });

        var svar = await http.PostAsync("https://api-test.tripletex.tech/v2/order", new StringContent("{\"a\":1}"));

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Equal(2, forsok);
        Assert.All(falsk.Kall, k => Assert.Equal("{\"a\":1}", k.Body));
    }

    [Fact]
    public void Ventetid_folger_Tripletex_men_har_tak()
    {
        var lang = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        lang.Headers.Add("Retry-After", "120");
        Assert.Equal(TimeSpan.FromSeconds(6), TripletexBegrensningHandler.Ventetid(lang, 1));

        var uten = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        Assert.Equal(TimeSpan.FromSeconds(4), TripletexBegrensningHandler.Ventetid(uten, 2));
    }

    // Fakturert inne i Tripletex: en faktura peker på ordren. Da blir arbeidsordren
    // Fakturert i portalen, og en betalt akonto blir Betalt. Kreditnota teller ikke.
    [Fact]
    public async Task Fakturert_i_Tripletex_settes_som_fakturert_i_portalen()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();

        var prosjekt = new Prosjekt { Navn = "Egersund" };
        db.Prosjekter.Add(prosjekt);
        var fakturert = new Arbeidsordre { Tittel = "Fakturert", Status = ArbeidsordreStatus.Ferdig, TripletexOrdreId = 500, TripletexOrdreSendtDato = DateTime.Today };
        var ikkeEnna = new Arbeidsordre { Tittel = "Ikke ennå", Status = ArbeidsordreStatus.Ferdig, TripletexOrdreId = 501, TripletexOrdreSendtDato = DateTime.Today };
        var kreditert = new Arbeidsordre { Tittel = "Kreditnota", Status = ArbeidsordreStatus.Ferdig, TripletexOrdreId = 502, TripletexOrdreSendtDato = DateTime.Today };
        db.Arbeidsordre.AddRange(fakturert, ikkeEnna, kreditert);
        await db.SaveChangesAsync();
        var akonto = new ProsjektAkonto { ProsjektId = prosjekt.Id, Nr = 1, Milepael = "Oppstart", Status = AkontoStatus.Sendt, SendtDato = DateTime.Today, TripletexOrdreId = 600 };
        db.ProsjektAkontoer.Add(akonto);
        await db.SaveChangesAsync();

        var idag = DateTime.Today.ToString("yyyy-MM-dd");
        var falsk = new FalskTripletex((req, _) => req.RequestUri!.PathAndQuery.Contains("token/session")
            ? Json("""{"value":{"token":"t"}}""")
            : Json("{\"values\":["
                + "{\"id\":1,\"invoiceNumber\":2041,\"invoiceDate\":\"" + idag + "\",\"amount\":5946,\"amountOutstanding\":5946,\"isCreditNote\":false,\"orders\":[{\"id\":500,\"number\":\"10255\"}]},"
                + "{\"id\":2,\"invoiceNumber\":2042,\"invoiceDate\":\"" + idag + "\",\"amount\":-100,\"amountOutstanding\":0,\"isCreditNote\":true,\"orders\":[{\"id\":502,\"number\":\"10257\"}]},"
                + "{\"id\":3,\"invoiceNumber\":2043,\"invoiceDate\":\"" + idag + "\",\"amount\":155000,\"amountOutstanding\":0,\"isCreditNote\":false,\"orders\":[{\"id\":600,\"number\":\"10300\"}]}"
                + "]}"));

        var r = await new TripletexSyncService(db, Lag(falsk)).OppdaterFakturertStatusAsync();

        Assert.Null(r.Feil);
        Assert.Equal(1, r.ArbeidsordreFakturert);
        Assert.Equal(1, r.AkontoerBetalt);
        Assert.Equal(ArbeidsordreStatus.Fakturert, fakturert.Status);
        Assert.Equal("Tripletex, faktura 2041", fakturert.FakturertAvNavn);
        Assert.Equal(ArbeidsordreStatus.Ferdig, ikkeEnna.Status);
        Assert.Equal(ArbeidsordreStatus.Ferdig, kreditert.Status);
        Assert.Equal(AkontoStatus.Betalt, akonto.Status);
    }

    // API-nøkkel fra Selskap → API-nøkler (tlxr_): byttes inn via :createFromRefreshToken
    // med ttlSeconds (maks 8 timer), uten consumer-token, og sesjonen brukes som vanlig.
    [Fact]
    public async Task Api_nokkel_tlxr_logger_inn_med_refresh_token_uten_consumer_token()
    {
        var falsk = new FalskTripletex((req, _) => req.RequestUri!.PathAndQuery.Contains("createFromRefreshToken")
            ? Json("""{"value":{"token":"jwt-sesjon"}}""")
            : Json("""{"value":{"companyId":93903281,"employee":{"firstName":"Andreas","lastName":"Karlsen"},"company":{"name":"AK transport"}}}"""));
        var nokkel = "tlxr_" + Guid.NewGuid().ToString("N");
        var tx = new TripletexService(new HttpClient(falsk) { BaseAddress = new Uri("https://api-test.tripletex.tech/v2/") },
            new TripletexService.Nokler("", nokkel, true), 1);

        var svar = await tx.TestTilkoblingAsync();

        Assert.True(svar.Ok, svar.Feilmelding);
        Assert.Equal("AK transport (konto 93903281)", svar.FirmaNavn);
        var inn = falsk.Kall.Single(k => k.Sti.Contains("createFromRefreshToken"));
        var body = JsonDocument.Parse(inn.Body!).RootElement;
        Assert.Equal(nokkel, body.GetProperty("refreshToken").GetString());
        Assert.Equal(28800, body.GetProperty("ttlSeconds").GetInt32());
        Assert.DoesNotContain(falsk.Kall, k => k.Sti.EndsWith("token/session/:create"));
    }

    [Theory]
    [InlineData("tlxr_abc", true)]
    [InlineData("  tlxr_abc  ", true)]
    [InlineData("eyJhbGciOi", false)]
    [InlineData(null, false)]
    public void Api_nokkel_kjennes_igjen_pa_tlxr(string? verdi, bool forventet) =>
        Assert.Equal(forventet, TripletexService.ErApiNokkelVerdi(verdi));
}
