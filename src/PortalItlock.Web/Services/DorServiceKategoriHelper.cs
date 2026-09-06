using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

public static class ServiceKategori
{
    public const string Mekanisk = "Mekanisk";
    public const string ElektriskSluttstykke = "Elektrisk sluttstykke";
    public const string Automatikk = "Dørautomatikk";
    public const string Romningsdorer = "Rømningsdører";

    public static readonly string[] Rekkefolge = [Mekanisk, ElektriskSluttstykke, Automatikk, Romningsdorer];

    public static string? Undertekst(string kategori) => kategori switch
    {
        Automatikk => "inkl. resertifisering av CE",
        _ => null
    };
}

public static class DorServiceKategoriHelper
{
    public static string KategoriserDor(Dor d)
    {
        var navn = d.Funksjoner.Select(f => f.Navn.ToLowerInvariant()).ToList();

        if (navn.Any(n => n.Contains("automatikk")))
        {
            return ServiceKategori.Automatikk;
        }

        if (navn.Any(n => n.Contains("rømning") || n.Contains("panikk")))
        {
            return ServiceKategori.Romningsdorer;
        }

        if (navn.Any(n => n.Contains("elektrisk sluttstykke")))
        {
            return ServiceKategori.ElektriskSluttstykke;
        }

        return ServiceKategori.Mekanisk;
    }
}
