namespace PortalItlock.Web.Models;

public sealed record Lenke(string Tittel, string Beskrivelse, string Href, string IconNavn = "folder");
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
                    new Lenke("Befaring", "Befaringsliste og utskiftning av lås", "befaringsmodul", "eye"),
                    new Lenke("Guider", "Kobling, dørmiljø og oppsett adgangskontroll", "guidermodul", "tool"),
                    new Lenke("Prosjekt", "Prosjekter og arbeidsordre", "prosjektmodul", "folder"),
                    new Lenke("Timer og fravær", "Fravær og timeregistrering", "timerfravarmodul", "clock"),
                    new Lenke("Utskiftning av låskasse", "Søk opp gammel lås, se hva du trenger", "lasekasse-utskiftning", "tool"),
                ]),
                new LenkeGruppe("Kart og kalender", [
                    new Lenke("Min dag", "Dagens jobber, adresse og varer å pakke, samlet", "min-dag", "calendar"),
                    new Lenke("Kart", "Se dagens jobber geografisk", "kart", "map"),
                    new Lenke("Kalender", "Se planlagte arbeidsordre i månedsvisning", "kalender", "calendar"),
                ]),
            ]),
        new Rolle(
            "prosjektledere",
            "For prosjektledere",
            "Systemer, prosjektering og prosjekter",
            [
                new LenkeGruppe("Prosjekt modul", [
                    new Lenke("Prosjekt modul", "Prosjekter, dørpakker, prisoverslag og systemregister samlet på ett sted", "prosjekteringmodul", "folder"),
                ]),
                new LenkeGruppe("Kunder", [
                    new Lenke("Kunder", "Kunderegister, oppfølging, portaltilgang og tilvalg samlet på ett sted", "kundemodul", "users"),
                ]),
                new LenkeGruppe("Drift", [
                    new Lenke("Drift", "Kart, kalender og fravær samlet på ett sted", "driftmodul", "map"),
                    new Lenke("Ressursplanlegger", "Sett av montører på dager fremover, vises i deres kalender", "ressursplanlegger", "calendar"),
                ]),
                new LenkeGruppe("Forespørsler", [
                    new Lenke("Forespørsler", "E-poster sendt til post@itlock.no", "foresporsler", "mail"),
                ]),
                new LenkeGruppe("Tickets", [
                    new Lenke("Tickets", "Samlet oversikt over alle henvendelser og saker", "tickets", "tag"),
                ]),
                new LenkeGruppe("Service modul", [
                    new Lenke("Service modul", "Serviceavtaler, serviceoppdrag og tilbud service samlet på ett sted", "servicemodul", "clock"),
                ]),
            ]),
        new Rolle(
            "admin",
            "For admin",
            "Opprett og vedlikehold grunnlagsdata",
            [
                new LenkeGruppe("Oppsett og maler", [
                    new Lenke("Oppsett og maler", "Dørpakker, krav, sjekklister, tilvalg og dokumenter samlet på ett sted", "oppsettmodul", "settings"),
                ]),
                new LenkeGruppe("Komponenter og priser", [
                    new Lenke("Komponenter og priser", "Komponentregister, prisimport, rabattgrupper og lagerstyring samlet på ett sted", "komponentprismodul", "box"),
                ]),
                new LenkeGruppe("Brukere og kunder", [
                    new Lenke("Brukere og kunder", "Brukere, kunder, kundeoppfølging og fravær samlet på ett sted", "brukerkundemodul", "users"),
                ]),
            ]),
    ];
}
