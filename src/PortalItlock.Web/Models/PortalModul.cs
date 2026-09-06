namespace PortalItlock.Web.Models;

public sealed record Lenke(string Tittel, string Beskrivelse, string Href);
public sealed record LenkeGruppe(string Navn, List<Lenke> Lenker);
public sealed record Rolle(string Nokkel, string Tittel, string Beskrivelse, List<LenkeGruppe> Grupper);

public static class PortalModulRegister
{
    public static readonly List<Rolle> Roller =
    [
        new Rolle(
            "montorer",
            "For montører",
            "Guide, befaring og vedlegg til bruk i felt",
            [
                new LenkeGruppe("Moduler", [
                    new Lenke("Befaring", "Befaringsliste og utskiftning av lås", "befaringsmodul"),
                    new Lenke("Guider", "Kobling, dørmiljø og oppsett adgangskontroll", "guidermodul"),
                    new Lenke("Prosjekt", "Prosjekter og arbeidsordre", "prosjektmodul"),
                    new Lenke("Timer og fravær", "Fravær og timeregistrering", "timerfravarmodul"),
                ]),
                new LenkeGruppe("Kart og kalender", [
                    new Lenke("Kart", "Se dagens jobber geografisk", "kart"),
                    new Lenke("Kalender", "Se planlagte arbeidsordre i månedsvisning", "kalender"),
                ]),
            ]),
        new Rolle(
            "prosjektledere",
            "For prosjektledere",
            "Systemer, prosjektering og prosjekter",
            [
                new LenkeGruppe("Prosjekt modul", [
                    new Lenke("Prosjekt modul", "Prosjekter, dørpakker, prisoverslag og systemregister samlet på ett sted", "prosjekteringmodul"),
                ]),
                new LenkeGruppe("Kunder", [
                    new Lenke("Kunder", "Kunderegister, oppfølging, portaltilgang og tilvalg samlet på ett sted", "kundemodul"),
                ]),
                new LenkeGruppe("Drift", [
                    new Lenke("Drift", "Kart, kalender og fravær samlet på ett sted", "driftmodul"),
                ]),
                new LenkeGruppe("Forespørsler", [
                    new Lenke("Forespørsler", "E-poster sendt til post@itlock.no", "foresporsler"),
                ]),
                new LenkeGruppe("Service modul", [
                    new Lenke("Service modul", "Serviceavtaler, serviceoppdrag og tilbud service samlet på ett sted", "servicemodul"),
                ]),
            ]),
        new Rolle(
            "admin",
            "For admin",
            "Opprett og vedlikehold grunnlagsdata",
            [
                new LenkeGruppe("Oppsett og maler", [
                    new Lenke("Oppsett og maler", "Dørpakker, krav, sjekklister, tilvalg og dokumenter samlet på ett sted", "oppsettmodul"),
                ]),
                new LenkeGruppe("Komponenter og priser", [
                    new Lenke("Komponenter og priser", "Komponentregister, prisimport, rabattgrupper og lagerstyring samlet på ett sted", "komponentprismodul"),
                ]),
                new LenkeGruppe("Brukere og kunder", [
                    new Lenke("Brukere og kunder", "Brukere, kunder, kundeoppfølging og fravær samlet på ett sted", "brukerkundemodul"),
                ]),
            ]),
    ];
}
