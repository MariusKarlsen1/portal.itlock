using PortalItlock.Web.Services;
using static PortalItlock.Web.Services.OpsjonDorOrden;

namespace PortalItlock.Web.Tests;

// Opsjonstilbud: rekkefølge, gruppering og overskrift på dørene.
public class OpsjonDorOrdenTests
{
    private static readonly List<DorInfo> Dorer =
    [
        new(1, "D-10", "Kontor", "2. etg", "Kontor standard"),
        new(2, "D-9", "Vindfang", "1. etg", "Inngangsparti"),
        new(3, "D-101", "Lager", "1. etg", null),
        new(4, "D-2", "Hovedinngang", "1. etg", "Inngangsparti"),
        new(5, "D-3", "Trapp", null, null),
    ];

    private static string Ider(IEnumerable<DorInfo> l) => string.Join(",", l.Select(d => d.Dornummer));

    [Fact]
    public void Standard_ErDorIdMedTalliRiktigRekkefolge() =>
        Assert.Equal("D-2,D-3,D-9,D-10,D-101", Ider(Ordne(Dorer, null, null)));

    [Fact]
    public void Synkende_OgToNivaer() =>
        Assert.Equal("D-2,D-9,D-101,D-10,D-3", Ider(Ordne(Dorer, "etasje,id", null)));

    [Fact]
    public void Grupper_TommeSist_SorteringInnenforGruppen()
    {
        Assert.Equal("D-9,D-2,D-10,D-3,D-101", Ider(Ordne(Dorer, "-navn", "pakke")));
        Assert.Equal("D-2,D-9,D-101,D-10,D-3", Ider(Ordne(Dorer, null, "etasje,pakke")));
    }

    [Fact]
    public void Overskrift_StandardErSomFor()
    {
        Assert.Equal("D-10 Kontor", Overskrift(Dorer[0], null));
        Assert.Equal("D-10 Kontor · 2. etg · Kontor standard", Overskrift(Dorer[0], "id,navn,etasje,pakke"));
        Assert.Equal("Kontor · Kontor standard", Overskrift(Dorer[0], "navn,pakke"));
        Assert.Equal("D-3", Overskrift(Dorer[4], "etasje"));
    }

    [Fact]
    public void Lesing_TaalerRot()
    {
        Assert.Equal([("id", true)], LesSortering("tull"));
        Assert.Equal([("navn", false), ("id", true)], LesSortering("-navn,id"));
        Assert.Equal(["etasje"], LesGruppering("etasje,etasje,id"));
        Assert.Equal("-navn,etasje", LagSortering([("navn", false), ("etasje", true)]));
    }
}
