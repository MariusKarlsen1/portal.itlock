using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Innkommende e-post fordeles på organisasjonen ut fra mottakeradressen.
public class ResendInboundParserTests
{
    [Fact]
    public void HentMottakere_LeserToCcOgReceivedFor_SomRenAdresseMedSmaBokstaver()
    {
        const string json = """
            {
              "type": "email.received",
              "data": {
                "to": ["Itlock AS <Itlock@Full-Kontroll.no>"],
                "cc": [],
                "bcc": [],
                "received_for": ["bryne-lasservice@full-kontroll.no"]
              }
            }
            """;

        var mottakere = ResendInboundParser.HentMottakere(json);

        Assert.Contains("itlock@full-kontroll.no", mottakere);
        Assert.Contains("bryne-lasservice@full-kontroll.no", mottakere);
    }
}
