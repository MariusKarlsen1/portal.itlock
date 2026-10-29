using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Tests;

// Import av dører fra prosjektets BIM-modell (IFC): hvilket felt som er
// dørnummeret, etasjenavn og kobling mot dørene som allerede er i portalen.
public class BimModellServiceTests
{
    private static BimModellService.IfcDor Ifc(string guid, string? tag, string? etasje = "Plan 01", string? brann = null, int? bredde = 1000, string? mark = null)
    {
        var d = new BimModellService.IfcDor { Guid = guid, Tag = tag, Navn = "Dør", Etasje = etasje, Bredde = bredde, Hoyde = 2100 };
        if (brann is not null) d.Egenskaper["FireRating"] = brann;
        if (mark is not null) d.Egenskaper["Mark"] = mark;
        return d;
    }

    [Theory]
    [InlineData("Plan 01", "1")]
    [InlineData("1. etasje", "1")]
    [InlineData("Etasje 2", "2")]
    [InlineData("Level 3", "3")]
    [InlineData("02", "2")]
    [InlineData("U1", "U1")]
    [InlineData("Loft", "Loft")]
    public void Etasje_GjorOmVanligeNavn(string navn, string forventet) =>
        Assert.Equal(forventet, BimModellService.Etasje(navn));

    [Fact]
    public void Nummerkilder_VelgerFeltetSomStemmerMedPortalen()
    {
        // Revit: Tag er element-ID, dørnummeret står i «Mark».
        var fra = new List<BimModellService.IfcDor> { Ifc("a", "312455", mark: "D-201"), Ifc("b", "312460", mark: "D-202") };
        var kilder = BimModellService.Nummerkilder(fra, ["D-201", "D-202"]);
        Assert.Equal("egenskap:Mark", kilder[0].Kode);
        Assert.Equal(2, kilder[0].Treff);
    }

    [Fact]
    public void Sammenlign_KoblerPaNummer_OgFinnerNyeOgEndrede()
    {
        var dorer = new List<Dor>
        {
            new() { Id = 1, ProsjektId = 1, Dornummer = "D-201", Brann = "", Bredde = 1000 },
            new() { Id = 2, ProsjektId = 1, Dornummer = "d-202", Brann = "EI30", Bredde = 1000 },
        };
        var fra = new List<BimModellService.IfcDor>
        {
            Ifc("g1", "D-201"),
            Ifc("g2", "D-202", brann: "EI60"),
            Ifc("g3", "D-209", etasje: "Plan 02", bredde: 900),
        };

        var r = BimModellService.Sammenlign(fra, dorer, "tag");

        var ny = Assert.Single(r, f => f.Type == BimModellService.Forslagstype.Ny);
        Assert.Equal("D-209", ny.Nummer);
        Assert.Equal("2", ny.Etasje);
        Assert.True(ny.Valgt);

        var endret = Assert.Single(r, f => f.Type == BimModellService.Forslagstype.Endret);
        Assert.Equal(2, endret.Dor!.Id);
        Assert.Contains("Brann EI30 → EI60", endret.Endringer);
        Assert.False(endret.Valgt);

        Assert.Equal(1, Assert.Single(r, f => f.Type == BimModellService.Forslagstype.Koblet).Dor!.Id);
    }

    [Fact]
    public void Sammenlign_GlobalIdVinnerOverNummer()
    {
        // Døra er allerede koblet til modellen, men har fått nytt nummer i portalen.
        var dorer = new List<Dor> { new() { Id = 7, ProsjektId = 1, Dornummer = "A-1", IfcGuid = "g1" } };
        var r = BimModellService.Sammenlign([Ifc("g1", "B-9")], dorer, "tag");
        Assert.Equal(7, Assert.Single(r).Dor!.Id);
    }
}
