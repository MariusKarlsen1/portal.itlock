namespace PortalItlock.Web.Models;

// Livsløpet til komponentene på en dør: når en vare ble lagt til, montert,
// fjernet eller fikk endret antall. Skrives automatisk av ApplicationDbContext
// ved lagring, uansett hvor endringen gjøres fra, og overskrives aldri. Navnet
// lagres som det var, så historikken står seg selv om varen endres senere.
// Bevisst uten fremmednøkkel til døren - loggen er historikk og skal ikke
// stoppe eller rydde noe ved sletting.
public class DorKomponentLogg
{
    public int Id { get; set; }
    public int DorId { get; set; }
    public int? ComponentId { get; set; }
    public required string Navn { get; set; }
    public string? Produktkode { get; set; }
    public string? Komponenttype { get; set; }
    public string Hendelse { get; set; } = DorKomponentHendelse.LagtTil;
    public int Antall { get; set; }
    public DateTime Tidspunkt { get; set; } = DateTime.Now;
    public int? BrukerId { get; set; }
    public string? Merknad { get; set; }
}

public static class DorKomponentHendelse
{
    public const string LagtTil = "LagtTil";
    public const string Fjernet = "Fjernet";
    public const string Montert = "Montert";
    public const string AntallEndret = "AntallEndret";

    public static string Tekst(string hendelse) => hendelse switch
    {
        LagtTil => "Lagt til",
        Fjernet => "Fjernet",
        Montert => "Montert",
        AntallEndret => "Antall endret",
        _ => hendelse
    };
}

// Fast kopi av alle prisreglene slik de var da de ble lagret. Avtaler lagrer
// hvilken versjon den godkjente prisen bygger på (Prosjekt.ServiceGodkjentRegelversjon),
// så gamle avtaler og tilbud kan forklares selv om reglene endres senere.
public class ServicePrisRegelVersjon
{
    public int Id { get; set; }
    public int Versjon { get; set; }
    public DateTime Dato { get; set; } = DateTime.Now;
    public string? AvNavn { get; set; }
    public int AntallRegler { get; set; }
    public required string Innhold { get; set; }
}
