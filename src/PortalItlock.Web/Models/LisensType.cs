namespace PortalItlock.Web.Models;

// Det en organisasjon kan gå tom for og trenge flere av. Én lisenstype per
// rolle: admin, prosjektleder, montør og driftslisens (vaktmester), og
// kundetilganger for deres egne kunder (opprettes på /kundetilganger).
//
// Brukere var én felles lisens for alle ansatte. Den er delt opp i Admin,
// Prosjektleder og Montor (se LisensOmlegging), men verdien beholdes så
// gamle rader i databasen fortsatt kan leses.
public enum LisensType
{
    Brukere = 0,
    KundeTilgang = 1,
    Admin = 2,
    Prosjektleder = 3,
    Montor = 4,
    Driftslisens = 5
}

public static class LisensTypeExtensions
{
    // Typene som er i bruk, i den rekkefølgen de vises.
    public static readonly LisensType[] Aktive =
        [LisensType.Admin, LisensType.Prosjektleder, LisensType.Montor, LisensType.Driftslisens, LisensType.KundeTilgang];

    // Lisensene for ansatte (ikke kunder og vaktmestere).
    public static readonly LisensType[] Ansatte = [LisensType.Admin, LisensType.Prosjektleder, LisensType.Montor];

    public static string Visningsnavn(this LisensType type) => type switch
    {
        LisensType.Brukere => "Brukerlisenser (gammel)",
        LisensType.KundeTilgang => "Kundetilganger",
        LisensType.Admin => "Adminlisenser",
        LisensType.Prosjektleder => "Prosjektlederlisenser",
        LisensType.Montor => "Montørlisenser",
        LisensType.Driftslisens => "Driftslisenser",
        _ => type.ToString()
    };

    public static string Kortnavn(this LisensType type) => type switch
    {
        LisensType.KundeTilgang => "Kundetilganger",
        LisensType.Admin => "Admin",
        LisensType.Prosjektleder => "Prosjektleder",
        LisensType.Montor => "Montør",
        LisensType.Driftslisens => "Driftslisens",
        _ => "Brukere"
    };

    public static string Beskrivelse(this LisensType type) => type switch
    {
        LisensType.KundeTilgang => "Kunder med innlogging i kundeportalen",
        LisensType.Admin => "Full tilgang, inkludert administrasjon",
        LisensType.Prosjektleder => "Prosjekter, tilbud, økonomi og planlegging",
        LisensType.Montor => "Min dag, arbeidsordre, timer og montering",
        LisensType.Driftslisens => "Vaktmestere og driftsansvarlige i driftsportalen",
        _ => "Ansatte med innlogging"
    };

    // Hvilken lisens en bruker med denne rollen trekker fra.
    public static LisensType ForRolle(BrukerRolle rolle) => rolle switch
    {
        BrukerRolle.Admin => LisensType.Admin,
        BrukerRolle.Prosjektleder => LisensType.Prosjektleder,
        BrukerRolle.Kunde => LisensType.KundeTilgang,
        BrukerRolle.Driftslisens => LisensType.Driftslisens,
        _ => LisensType.Montor
    };

    public static BrukerRolle? Rolle(this LisensType type) => type switch
    {
        LisensType.Admin => BrukerRolle.Admin,
        LisensType.Prosjektleder => BrukerRolle.Prosjektleder,
        LisensType.Montor => BrukerRolle.Montor,
        LisensType.Driftslisens => BrukerRolle.Driftslisens,
        LisensType.KundeTilgang => BrukerRolle.Kunde,
        _ => null
    };
}
