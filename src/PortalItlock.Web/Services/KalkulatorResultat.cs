namespace PortalItlock.Web.Services;

// Det en kalkulator sender til tilbudet når man trykker «Legg til i tilbud»:
// en overskrift (med beskrivelse) og varelinjene under den.
public sealed record KalkulatorLinje(string Kode, string Navn, int Antall, decimal Netto, decimal? Veiledende);

public sealed record KalkulatorResultat(string Kalkulator, string Overskrift, string? Beskrivelse, IReadOnlyList<KalkulatorLinje> Linjer);
