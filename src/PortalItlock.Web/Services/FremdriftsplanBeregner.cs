using System.Globalization;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Fremdriftsplanen fra sjekklisten i økonomiarket («Kalkulering og
// økonomi.xlsx»), fra prosjektet er vunnet og registrert. Tilbuds- og
// kalkulasjonsfasen i arket er ikke med, den er ferdig før prosjektet havner
// her. Fasene regnes fra tre datoer på prosjektet:
//   Planlegging:   prosjektstart    → dagen før montasjestart
//   Gjennomføring: montasjestart    → overlevering − 7 dager (som i arket)
//   Avslutning:    overlevering − 7 → overlevering
// Aktivitetene ligger som andeler av fasen, så alt flytter seg med fristene.
public static class FremdriftsplanBeregner
{
    public static readonly string[] FaseNavn = ["Planlegging", "Gjennomføring", "Avslutning"];

    public static readonly string[] Roller = ["Prosjektsjef", "Prosjektleder", "BAS", "Montør", "Daglig leder"];

    public sealed record Fase(int Nr, string Navn, DateTime Fra, DateTime Til);

    public sealed record Frister(DateTime Start, DateTime Montasjestart, DateTime Overlevering);

    // Fristene for prosjektet, med fornuftige standardverdier når noe mangler.
    // Null når verken montasjestart eller overlevering er satt.
    public static Frister? FristerFor(Prosjekt p, int fristforlengelseDager = 0)
    {
        if (p.MontasjestartDato is null && p.OverleveringsfristDato is null)
        {
            return null;
        }

        var start = (p.StartDato ?? p.OpprettetDato.ToLocalTime()).Date;
        var montasje = (p.MontasjestartDato ?? start.AddDays(21)).Date;
        var overlevering = (p.OverleveringsfristDato ?? montasje.AddDays(56)).Date.AddDays(fristforlengelseDager);
        return new Frister(Min(start, montasje), montasje, overlevering);
    }

    public static List<Fase> Faser(Frister f) =>
    [
        new(0, FaseNavn[0], f.Start, Maks(f.Start, f.Montasjestart.AddDays(-1))),
        new(1, FaseNavn[1], f.Montasjestart, Maks(f.Montasjestart, f.Overlevering.AddDays(-7))),
        new(2, FaseNavn[2], Maks(f.Montasjestart, f.Overlevering.AddDays(-7)), f.Overlevering),
    ];

    public static Fase FaseFor(List<Fase> faser, int fase) => faser[Math.Clamp(fase, 0, faser.Count - 1)];

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

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    // ---------- Malen ----------

    public sealed record MalOppgave(string Tekst, string Rolle);

    public sealed record MalAktivitet(int Fase, string Navn, string Rolle, decimal DelFra, decimal DelTil, MalOppgave[] Oppgaver);

    private static MalOppgave O(string tekst, string rolle) => new(tekst, rolle);

    // Sjekkliste-malen fra arket, fra kontrakten er signert. Det som skal
    // gjøres rett etter kontrakt (faktureringsplan, bankgaranti, dokumentasjon)
    // ligger først i Planlegging.
    public static readonly MalAktivitet[] Mal =
    [
        new(0, "Etter kontrakt", "Prosjektleder", 0m, .3m,
        [
            O("Lag faktureringsplan og oppdater ordrereserve", "Prosjektleder"),
            O("Sikre bankgaranti og forsikring", "Daglig leder"),
            O("Dokumentasjon til kunden: ansvarsrett, risikovurdering, HMS, KS-perm", "Prosjektleder"),
            O("Avklar prøvedrift, garantiperiode og overlevering", "Prosjektleder"),
            O("Informasjon til økonomi", "Prosjektleder"),
            O("Definer BAS og mannskap", "Prosjektsjef"),
        ]),
        new(0, "Forberedelser", "Prosjektleder", .2m, .7m,
        [
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
        new(0, "Oppstart", "Prosjektleder", .6m, .95m,
        [
            O("Gå gjennom befaringssjekklisten for avvik", "BAS"),
            O("Informer mannskapet om innsjekk og byggekort", "BAS"),
            O("Småmateriell tilgjengelig", "BAS"),
            O("Fordel roller i mannskapet", "BAS"),
            O("Bestill leieutstyr (trappetrall, lift)", "BAS"),
            O("Startbefaring med mannskapet", "BAS"),
            O("Oppstartsmøte med kunden", "Prosjektleder"),
        ]),
        new(0, "Igangsetting", "Prosjektleder", .85m, 1m,
        [
            O("Utbedre avvik", "BAS"),
            O("Leveransekontroll av produkter (mengde og kvalitet)", "BAS"),
            O("Tilhenger og lager avklart", "BAS"),
            O("Innsjekk på byggeplass (PSI, byggekort, signering)", "Montør"),
        ]),
        new(1, "Administrativ", "Prosjektleder", 0m, 1m,
        [
            O("Kontroll på tidsplan, fremdrift og bestillinger", "Prosjektleder"),
            O("Innhent sjekklister og bilder", "Prosjektleder"),
            O("Handlingsplan for avvik", "Prosjektleder"),
            O("Samme mannskap gjennom hele fasen, og plan B", "Prosjektleder"),
            O("Besøk byggeplassen", "Prosjektleder"),
            O("Avtal tid for montering med kunden", "Prosjektleder"),
        ]),
        new(1, "Montering", "BAS", .03m, .85m,
        [
            O("Førkontroll utført", "BAS"),
            O("Kontroll på endringer i tidsplan og ressurser", "Prosjektleder"),
            O("Følg opp småmateriell", "Montør"),
            O("Rapporter ekstra behov", "BAS"),
            O("Bruk av hjelpemidler (trappetrall, lift)", "Montør"),
        ]),
        new(1, "Kontroll", "BAS", .7m, 1m,
        [
            O("Fyll ut sjekklister", "BAS"),
            O("Test og kontroll av dører", "BAS"),
        ]),
        new(2, "Oppsummering", "Prosjektleder", 0m, .5m,
        [
            O("Innhent manglende FDV", "Prosjektleder"),
            O("Kontroller informasjonen fra hele prosjektet", "Prosjektleder"),
            O("Gå gjennom tillegg med montør", "Prosjektleder"),
        ]),
        new(2, "Overlevering", "Prosjektleder", .3m, 1m,
        [
            O("Testprotokoll", "Prosjektleder"),
            O("Godkjenning av prosjekt, KS og prøvedrift fra kunden", "Prosjektleder"),
            O("Overfør til service for garanti, reklamasjon og prøvedrift", "Prosjektleder"),
            O("Send manglende FDV", "Prosjektleder"),
            O("Sjekk status på servicekontrakt", "Prosjektsjef"),
            O("Nedskriv bankgaranti", "Daglig leder"),
            O("Insalg av nye prosjekter til kunden", "Prosjektleder"),
        ]),
        new(2, "Evaluering", "Prosjektsjef", .8m, 1m,
        [
            O("Etterkalkyle", "Prosjektsjef"),
            O("Informasjonsmøte om avsluttet prosjekt med alle involverte", "Prosjektsjef"),
            O("Avslutt bankgaranti", "Daglig leder"),
        ]),
    ];
}
