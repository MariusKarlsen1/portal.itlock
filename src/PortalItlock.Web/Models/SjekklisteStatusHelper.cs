namespace PortalItlock.Web.Models;

public static class SjekklisteStatusHelper
{
    public static (string Farge, string Tekst) Beregn(IReadOnlyCollection<ArbeidsordreSjekkpunkt> punkter)
    {
        if (punkter.Count == 0)
        {
            return ("#2f6fb3", "Ingen sjekkliste");
        }

        var fullfort = punkter.Count(p => p.Fullfort);
        if (fullfort == punkter.Count)
        {
            return ("#2b7a4b", "Sjekkliste fullført");
        }

        return ("#d9a520", $"{fullfort}/{punkter.Count} sjekket av");
    }
}
