namespace PortalItlock.Web.Models;

// De to tingene en organisasjon kan gå tom for og trenge flere av:
// Brukere = ansatte-/stab-kontoer (opprettes på /brukere), KundeTilgang =
// innloggingstilganger for deres egne kunder (opprettes på /kundetilganger).
public enum LisensType
{
    Brukere,
    KundeTilgang
}

public static class LisensTypeExtensions
{
    public static string Visningsnavn(this LisensType type) => type switch
    {
        LisensType.Brukere => "Brukerlisenser",
        LisensType.KundeTilgang => "Kundetilganger",
        _ => type.ToString()
    };
}
