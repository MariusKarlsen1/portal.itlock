using PortalItlock.Web.Models;
using Xunit;

namespace PortalItlock.Web.Tests;

// Lederveiene i en kabel er punkter på lerretet: flyttes eller limes kabelen inn,
// må de flyttes like mye, ellers blir lederendene stående igjen.
public class KoblingsKabelFlyttTests
{
    private static string Ledere() => KoblingsKabel.Skriv(
    [
        new KoblingsLeder { Farge = "Rød", Start = [new() { X = 10, Y = 20 }], StartAuto = true },
        new KoblingsLeder { Farge = "Sort", Slutt = [new() { X = 50, Y = 60 }, new() { X = 55, Y = 60 }] },
    ]);

    [Fact]
    public void Flytt_flytter_alle_lederpunkter()
    {
        var l = KoblingsKabel.Les(KoblingsKabel.Flytt(Ledere(), 5, -2));
        Assert.Equal(15, l[0].Start![0].X);
        Assert.Equal(18, l[0].Start![0].Y);
        Assert.Equal(60, l[1].Slutt![1].X);
        Assert.True(l[0].StartAuto);
    }

    [Fact]
    public void Skaler_rundt_midten()
    {
        var l = KoblingsKabel.Les(KoblingsKabel.Skaler(Ledere(), 50, 50, 2));
        Assert.Equal(-30, l[0].Start![0].X);
        Assert.Equal(70, l[1].Slutt![0].Y);
    }

    [Fact]
    public void Ren_flytting_oppdages_men_ikke_endret_form()
    {
        KoblingsStrekPunkt P(double x, double y) => new() { X = x, Y = y };
        Assert.Equal((3.0, 4.0), KoblingsKabel.RenFlytting([P(0, 0), P(10, 0)], [P(3, 4), P(13, 4)]));
        Assert.Null(KoblingsKabel.RenFlytting([P(0, 0), P(10, 0)], [P(3, 4), P(20, 4)]));
        Assert.Null(KoblingsKabel.RenFlytting([P(0, 0)], [P(0, 0)]));
    }

    [Fact]
    public void Vanlig_strek_uten_ledere_er_uendret() => Assert.Null(KoblingsKabel.Flytt(null, 1, 1));
}
