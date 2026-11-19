using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Når varene på en dør med pakke endres og man velger «bare denne døren»,
// får døren PakkeId 0: den regnes ikke som pakke lenger, selv om alle varene
// fra pakken fortsatt står på den.
public class DorPakkeBruttTests
{
    private static readonly DorPakkeService.PakkeInfo Pakke = new(5, "ID-2fl-Da-Kor-Bl", [1, 2], []);

    private static Dor DorMed(int? pakkeId, params int[] komp) => new()
    {
        Dornummer = "D1",
        PakkeId = pakkeId,
        Komponenter = komp.Select(k => new DorKomponent { ComponentId = k }).ToList()
    };

    [Fact]
    public void UtenPakkeId_KjennesIgjenPaVarene() =>
        Assert.Equal(5, DorPakkeService.PakkeFor(DorMed(null, 1, 2, 3), [Pakke])?.Id);

    [Fact]
    public void BruttPakke_RegnesIkkeSomPakke() =>
        Assert.Null(DorPakkeService.PakkeFor(DorMed(0, 1, 2, 3), [Pakke]));
}
