namespace PortalItlock.Web.Models;

public class DorFunksjon
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public string? Forkortelse { get; set; }

    public List<Dor> Dorer { get; set; } = [];
    public List<Package> Pakker { get; set; } = [];
}

// Dørfunksjonene står alltid i samme rekkefølge (som i dørfunksjonsregisteret:
// alfabetisk på navn), uansett hvilken rekkefølge de ble lagt på døren.
public static class DorFunksjonRekkefolge
{
    private static readonly StringComparer Navn = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("nb-NO"), true);

    public static IEnumerable<DorFunksjon> IFastRekkefolge(this IEnumerable<DorFunksjon> funksjoner) =>
        funksjoner.OrderBy(f => f.Navn, Navn).ThenBy(f => f.Id);
}
