using System.Globalization;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Fremdriftsplanen fra sjekklisten i økonomiarket («Kalkulering og
// økonomi.xlsx»). Fasene regnes fra fire datoer på prosjektet med de samme
// reglene som i arket:
//   Tilbud og kontrakt: start            → tilbudsfrist − 2 dager
//   Planlegging:        tilbudsfrist     → montasjestart − 7 dager
//   Gjennomføring:      montasjestart    → overlevering − 7 dager
//   Avslutning:         overlevering − 7 → overlevering
// Aktivitetene ligger som andeler av fasen, så alt flytter seg med fristene.
public static class FremdriftsplanBeregner
{
    public static readonly string[] FaseNavn = ["Tilbud og kontrakt", "Planlegging", "Gjennomføring", "Avslutning"];

    public static readonly string[] Roller = ["Prosjektsjef", "Kalkulatør", "Prosjektleder", "BAS", "Montør", "Daglig leder", "Servicesjef"];

    public sealed record Fase(int Nr, string Navn, DateTime Fra, DateTime Til);

    public sealed record Frister(DateTime Start, DateTime Tilbudsfrist, DateTime Montasjestart, DateTime Overlevering);

    // Fristene for prosjektet, med fornuftige standardverdier når noe mangler.
    // Null når ingen av datoene er satt.
    public static Frister? FristerFor(Prosjekt p, int fristforlengelseDager = 0)
    {
        if (p.MontasjestartDato is null && p.OverleveringsfristDato is null && p.TilbudsfristDato is null)
        {
            return null;
        }

        var start = (p.StartDato ?? p.OpprettetDato.ToLocalTime()).Date;
        var tilbud = (p.TilbudsfristDato ?? start.AddDays(14)).Date;
        var montasje = (p.MontasjestartDato ?? tilbud.AddDays(21)).Date;
        var overlevering = (p.OverleveringsfristDato ?? montasje.AddDays(56)).Date.AddDays(fristforlengelseDager);
        return new Frister(start, tilbud, montasje, overlevering);
    }

    public static List<Fase> Faser(Frister f) =>
    [
        new(0, FaseNavn[0], f.Start, Maks(f.Start, f.Tilbudsfrist.AddDays(-2))),
        new(1, FaseNavn[1], f.Tilbudsfrist, Maks(f.Tilbudsfrist, f.Montasjestart.AddDays(-7))),
        new(2, FaseNavn[2], f.Montasjestart, Maks(f.Montasjestart, f.Overlevering.AddDays(-7))),
        new(3, FaseNavn[3], Maks(f.Montasjestart, f.Overlevering.AddDays(-7)), f.Overlevering),
    ];

    public static (DateTime Fra, DateTime Til) AktivitetDatoer(Fase fase, decimal delFra, decimal delTil)
    {
        var lengde = Math.Max(0, (fase.Til - fase.Fra).TotalDays);
        return (fase.Fra.AddDays(Math.Round(lengde * (double)Math.Clamp(delFra, 0, 1))),
                fase.Fra.AddDays(Math.Round(lengde * (double)Math.Clamp(delTil, 0, 1))));
    }

    // Som i arket: (avsluttet + ikke aktuelt) / antall oppgaver.
    public static decimal Andel(IEnumerable<PlanOppgaveStatus> statuser)
    {
        var liste = statuser.ToList();
        return liste.Count == 0 ? 0 : (decimal)liste.Count(s => s.ErFerdig()) / liste.Count;
    }

    public static DateTime Mandag(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    public static int Ukenr(DateTime d) => ISOWeek.GetWeekOfYear(d);

    // Hele uker mellom mandagene. WEEKNUM i arket blir feil over nyttår,
    // dette blir det ikke.
    public static int UkerMellom(DateTime fra, DateTime til) => (int)Math.Round((Mandag(til) - Mandag(fra)).TotalDays / 7);

    private static DateTime Maks(DateTime a, DateTime b) => a > b ? a : b;

    // ---------- Malen ----------

    public sealed record MalOppgave(string Tekst, string Rolle);

    public sealed record MalAktivitet(int Fase, string Navn, string Rolle, decimal DelFra, decimal DelTil, MalOppgave[] Oppgaver);

    private static MalOppgave O(string tekst, string rolle) => new(tekst, rolle);

    // Sjekkliste-malen fra arket, med de samme fasene, aktivitetene og rollene.
    public static readonly MalAktivitet[] Mal =
    [
        new(0, "Forespørsel eller anbud", "Kalkulatør", 0m, .3m,
        [
            O("Vurder samarbeid med kunden og prosjektet", "Prosjektsjef"),
            O("Opprett jobbnummer og timeordre", "Kalkulatør"),
            O("Innhent beskrivelse, tegninger, dør- og funksjonsbeskrivelse", "Kalkulatør"),
            O("Risikovurdering av omfang: økonomi, omdømme og kapasitet", "Prosjektsjef"),
            O("Vurder mulige leverandører", "Kalkulatør"),
            O("Vurder intern eller ekstern kompetanse og ressurs", "Kalkulatør"),
            O("Godkjenning av prosjektet", "Prosjektsjef"),
        ]),
        new(0, "Prosjektforståelse", "Kalkulatør", .2m, .5m,
        [
            O("Kvalitetssikre informasjonen og innhent det som mangler", "Kalkulatør"),
            O("Del informasjon med BAS og prosjektleder", "Kalkulatør"),
            O("Vurder ulike løsninger", "Kalkulatør"),
            O("Definer prosjektleder og BAS", "Prosjektsjef"),
            O("Sett opp kompetanse, ressursbehov og mannskap", "Prosjektleder"),
            O("Behov for utstyr og hjelpemidler (lift, kran, spesialverktøy)", "Prosjektleder"),
            O("Lagerplass, parkering, forhold på byggeplass og inntransport", "Prosjektleder"),
            O("Grensesnitt mot andre fag", "Kalkulatør"),
        ]),
        new(0, "Kalkulasjon", "Kalkulatør", .35m, .8m,
        [
            O("Innhent FDV-dokumentasjon, garanti og salgsbetingelser", "Kalkulatør"),
            O("Innhent priser fra flere leverandører", "Kalkulatør"),
            O("Beregn rigg og drift: materiell, mannskap, utstyr og plass", "Kalkulatør"),
            O("Innhent pris for ekstern montasje ved behov", "Kalkulatør"),
            O("Avklaring med kunden", "Kalkulatør"),
            O("Indeksregulering, eventuelt priset inn i fastpris", "Kalkulatør"),
            O("Sidemannskontroll", "Prosjektsjef"),
        ]),
        new(0, "Levere tilbud", "Kalkulatør", .75m, .95m,
        [
            O("Kvalitetssikret tilbud av kontrollør", "Prosjektsjef"),
            O("Tilbud med salgs- og leveringsbetingelser", "Kalkulatør"),
            O("FDV-dokumentasjon til kunden", "Kalkulatør"),
            O("Forslag til servicekontrakt", "Servicesjef"),
            O("Sjekk tilbudet mot kravspesifikasjonen", "Kalkulatør"),
            O("Send tilbudet med FDV til kunden", "Kalkulatør"),
        ]),
        new(0, "Avklaringsforhandling", "Prosjektsjef", .9m, 1m,
        [
            O("Signert kontrakt fra kunden", "Prosjektsjef"),
            O("Lag faktureringsplan og oppdater ordrereserve", "Prosjektleder"),
            O("Sikre bankgaranti og forsikring", "Daglig leder"),
            O("Dokumentasjon til kunden: ansvarsrett, risikovurdering, HMS, KS-perm", "Prosjektleder"),
            O("Avklar prøvedrift, garantiperiode og overlevering", "Prosjektleder"),
            O("Informasjon til økonomi", "Prosjektleder"),
            O("Prosjektmappe til intern overlevering", "Prosjektleder"),
        ]),
        new(1, "Forberedelser", "Prosjektleder", 0m, .6m,
        [
            O("Overleveringsmøte med kalkulatør og prosjektsjef", "Prosjektleder"),
            O("Lag tidsplan for gjennomføring sammen med BAS", "Prosjektleder"),
            O("Fyll ut timeordre med informasjon til BAS og montør", "Prosjektleder"),
            O("Lag bestillingsplan og bestill riktig produkt til riktig tid", "Prosjektleder"),
            O("Sikre at varer på lager ikke brukes i andre oppdrag", "BAS"),
            O("Fastsett ressursplan og plan B", "Prosjektleder"),
            O("Nødvendige kurs og politiattest", "Prosjektleder"),
            O("Sikre godkjente innleide", "Prosjektleder"),
            O("Sikkerhetsutstyr og godkjent verktøy", "Prosjektleder"),
            O("Lager, container eller rom på bygget", "Prosjektleder"),
            O("Befaring på byggeplass med BAS", "Prosjektleder"),
            O("SJA med BAS, montør og verneombud", "Prosjektleder"),
            O("IK- og KS-perm til kunden", "Prosjektleder"),
        ]),
        new(1, "Oppstart", "Prosjektleder", .5m, .9m,
        [
            O("Gå gjennom befaringssjekklisten for avvik", "BAS"),
            O("Informer mannskapet om innsjekk og byggekort", "BAS"),
            O("Småmateriell tilgjengelig", "BAS"),
            O("Fordel roller i mannskapet", "BAS"),
            O("Bestill leieutstyr (trappetrall, lift)", "BAS"),
            O("Startbefaring med mannskapet", "BAS"),
            O("Oppstartsmøte med kunden", "Prosjektleder"),
        ]),
        new(1, "Igangsetting", "Prosjektleder", .8m, 1m,
        [
            O("Utbedre avvik", "BAS"),
            O("Leveransekontroll av produkter (mengde og kvalitet)", "BAS"),
            O("Tilhenger og lager avklart", "BAS"),
            O("Innsjekk på byggeplass (PSI, byggekort, signering)", "Montør"),
        ]),
        new(2, "Administrativ", "Prosjektleder", 0m, 1m,
        [
            O("Kontroll på tidsplan, fremdrift og bestillinger", "Prosjektleder"),
            O("Innhent sjekklister og bilder", "Prosjektleder"),
            O("Handlingsplan for avvik", "Prosjektleder"),
            O("Samme mannskap gjennom hele fasen, og plan B", "Prosjektleder"),
            O("Besøk byggeplassen", "Prosjektleder"),
            O("Avtal tid for montering med kunden", "Prosjektleder"),
        ]),
        new(2, "Montering", "BAS", .03m, .85m,
        [
            O("Førkontroll utført", "BAS"),
            O("Kontroll på endringer i tidsplan og ressurser", "Prosjektleder"),
            O("Følg opp småmateriell", "Montør"),
            O("Rapporter ekstra behov", "BAS"),
            O("Bruk av hjelpemidler (trappetrall, lift)", "Montør"),
        ]),
        new(2, "Kontroll", "BAS", .7m, 1m,
        [
            O("Fyll ut sjekklister", "BAS"),
            O("Test og kontroll av dører", "BAS"),
        ]),
        new(3, "Oppsummering", "Prosjektleder", 0m, .5m,
        [
            O("Innhent manglende FDV", "Prosjektleder"),
            O("Kontroller informasjonen fra hele prosjektet", "Prosjektleder"),
            O("Gå gjennom tillegg med montør", "Prosjektleder"),
        ]),
        new(3, "Overlevering", "Prosjektleder", .3m, 1m,
        [
            O("Testprotokoll", "Prosjektleder"),
            O("Godkjenning av prosjekt, KS og prøvedrift fra kunden", "Prosjektleder"),
            O("Overfør til service for garanti, reklamasjon og prøvedrift", "Prosjektleder"),
            O("Send manglende FDV", "Prosjektleder"),
            O("Sjekk status på servicekontrakt", "Prosjektsjef"),
            O("Nedskriv bankgaranti", "Daglig leder"),
            O("Insalg av nye prosjekter til kunden", "Prosjektleder"),
        ]),
        new(3, "Evaluering", "Prosjektsjef", .8m, 1m,
        [
            O("Etterkalkyle", "Prosjektsjef"),
            O("Informasjonsmøte om avsluttet prosjekt med alle involverte", "Prosjektsjef"),
            O("Avslutt bankgaranti", "Daglig leder"),
        ]),
    ];
}
