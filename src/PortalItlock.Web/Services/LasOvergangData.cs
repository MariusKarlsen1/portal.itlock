using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Overgangstabell for utskiftning av gammel låskasse, hentet fra Habo sin
// "Overgangstabell Lås" (habo.com). Rent oppslagsverk - endres bare hvis Habo
// selv oppdaterer tabellen, derfor statisk data i stedet for databasetabell.
public static class LasOvergangData
{
    public const string MerknadMellomdorlaser = "x = gammel lås har 9mm vriderfalle. x* = fra 1975 levert med 8mm vriderfallrør.";
    public const string MerknadTrio = "* = kan også bruke erstatningslås 22501 (410).";
    public const string MerknadModullas = "OBS: ved utskiftning fra 51-serie lås til modullås kan det gamle langskiltet og sylinderen ikke passe - låsene har ulik senteravstand, og modullås har ikke frontfeste for sylinder.";

    public static readonly IReadOnlyList<LasOvergangRad> Rader =
    [
        // Mellomdørlåser
        new() { DuHarLas = "1341", Kategori = "Mellomdørlåser", Laskasse = "2214", Sylinder = "-", Annet = "x" },
        new() { DuHarLas = "1322", Kategori = "Mellomdørlåser", Laskasse = "2214", Sylinder = "-", Annet = "x" },
        new() { DuHarLas = "1362", Kategori = "Mellomdørlåser", Laskasse = "2214", Sylinder = "-", Annet = "x" },
        new() { DuHarLas = "D22", Kategori = "Mellomdørlåser", Laskasse = "2214", Sylinder = "-" },
        new() { DuHarLas = "V22", Kategori = "Mellomdørlåser", Laskasse = "2214", Sylinder = "-", Annet = "x" },
        new() { DuHarLas = "K22", Kategori = "Mellomdørlåser", Laskasse = "2214", Sylinder = "-", Annet = "x" },
        new() { DuHarLas = "KV22", Kategori = "Mellomdørlåser", Laskasse = "2214", Sylinder = "-", Annet = "x*" },
        new() { DuHarLas = "1323", Kategori = "Mellomdørlåser", Laskasse = "B-522", Sylinder = "44/5525/1225", Annet = "x" },
        new() { DuHarLas = "B22", Kategori = "Mellomdørlåser", Laskasse = "B-522", Sylinder = "44/5525/1225", Annet = "x*" },
        new() { DuHarLas = "S22", Kategori = "Mellomdørlåser", Laskasse = "B-522", Utskiftningsskilt = "7590", Sylinder = "44/5525/1225", Annet = "x*" },
        new() { DuHarLas = "VS22", Kategori = "Mellomdørlåser", Laskasse = "B-522", Sylinder = "44/5525/1225", Annet = "x*" },
        new() { DuHarLas = "S522", Kategori = "Mellomdørlåser", Laskasse = "B-522", Sylinder = "44/5525/1225", Annet = "x*" },
        new() { DuHarLas = "1122", Kategori = "Mellomdørlåser", Laskasse = "B-522", Sylinder = "44/5525/1225", Annet = "x*" },
        new() { DuHarLas = "2022", Kategori = "Mellomdørlåser", Laskasse = "2014", Sylinder = "-" },

        // TrioVing låser
        new() { DuHarLas = "4041/35", Kategori = "TrioVing låser", Laskasse = "5016", Utskiftningsskilt = "7649", Sylinder = "65520/5520/1220", Annet = "5976 langskilt" },
        new() { DuHarLas = "4211", Kategori = "TrioVing låser", Laskasse = "5316", Sylinder = "44/5525/1225", Alternativt = "565(1498), 8659(7606), 5545(evt.wc 7562/7669)" },
        new() { DuHarLas = "4212", Kategori = "TrioVing låser", Laskasse = "5316", Sylinder = "44/5525/1225", Alternativt = "565(1498), 8659(7606), 5545" },
        new() { DuHarLas = "B4212", Kategori = "TrioVing låser", Laskasse = "5316", Sylinder = "44/5525/1225", Alternativt = "565(1498), 8659(7606), 5545" },
        new() { DuHarLas = "4213", Kategori = "TrioVing låser", Laskasse = "565", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "4281", Kategori = "TrioVing låser", Laskasse = "5181", Utskiftningsskilt = "7606", Sylinder = "44/5525/1225" },
        new() { DuHarLas = "5022", Kategori = "TrioVing låser", Laskasse = "2016", Sylinder = "44/5525/1225" },
        new() { DuHarLas = "5212", Kategori = "TrioVing låser", Laskasse = "5316", Sylinder = "44/5525/1225", Alternativt = "565,8659,5545" },
        new() { DuHarLas = "5219", Kategori = "TrioVing låser", Laskasse = "8765", Utskiftningsskilt = "8659", Sylinder = "5537C/1237C" },
        new() { DuHarLas = "5221", Kategori = "TrioVing låser", Laskasse = "562", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5222", Kategori = "TrioVing låser", Laskasse = "562", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5231", Kategori = "TrioVing låser", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5232", Kategori = "TrioVing låser", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5233", Kategori = "TrioVing låser", Laskasse = "8561", Utskiftningsskilt = "8659", Sylinder = "5545C/1245C", Annet = "8560 nøddørskilt" },
        new() { DuHarLas = "5241", Kategori = "TrioVing låser", Laskasse = "5316", Sylinder = "44/5525/1225", Alternativt = "9788,7606,5537C, 8659SC" },
        new() { DuHarLas = "5251", Kategori = "TrioVing låser", Laskasse = "9788", Utskiftningsskilt = "8659SC", Sylinder = "5537C/1237C" },
        new() { DuHarLas = "5262", Kategori = "TrioVing låser", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5281", Kategori = "TrioVing låser", Laskasse = "9787", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245" },

        // Trio
        new() { DuHarLas = "1369*", Kategori = "Trio", Laskasse = "565", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe", Alternativt = "evt. wc-garn 7562/7669" },
        new() { DuHarLas = "1309*", Kategori = "Trio", Laskasse = "565**(1498)", Utskiftningsskilt = "8659 (7606)", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "17*", Kategori = "Trio", Laskasse = "565", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "1121*", Kategori = "Trio", Laskasse = "565", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "1131*", Kategori = "Trio", Laskasse = "562", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "1132*", Kategori = "Trio", Laskasse = "562", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "1141*", Kategori = "Trio", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "1142*", Kategori = "Trio", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "1145*", Kategori = "Trio", Laskasse = "8561", Utskiftningsskilt = "8659/7654", Sylinder = "5545C/1245C", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "1151*", Kategori = "Trio", Laskasse = "411", Utskiftningsskilt = "7606", Sylinder = "5537C/1237C", Annet = "5928 underlagsstolpe", Alternativt = "8659SC" },
        new() { DuHarLas = "1162*", Kategori = "Trio", Laskasse = "411", Utskiftningsskilt = "7606", Sylinder = "5537C/1237C", Annet = "5928 underlagsstolpe", Alternativt = "8659SC" },
        new() { DuHarLas = "1163*", Kategori = "Trio", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },
        new() { DuHarLas = "1158*", Kategori = "Trio", Laskasse = "9787", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245", Annet = "5928 underlagsstolpe" },

        // Ving
        new() { DuHarLas = "4011", Kategori = "Ving", Laskasse = "565", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245", Alternativt = "evt. wc-garn 7562/7669" },
        new() { DuHarLas = "4031", Kategori = "Ving", Laskasse = "565** (1498)", Utskiftningsskilt = "8659 (7606)", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "4041", Kategori = "Ving", Laskasse = "565", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5012", Kategori = "Ving", Laskasse = "565", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5021", Kategori = "Ving", Laskasse = "562", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5061", Kategori = "Ving", Laskasse = "562", Utskiftningsskilt = "8659", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5031", Kategori = "Ving", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5032", Kategori = "Ving", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5022", Kategori = "Ving", Laskasse = "8561*", Utskiftningsskilt = "8659 (7654)", Sylinder = "5545C/1245C", Merknad = "Kan også benytte B-522, da denne har samme mål. Ingen utskiftningsskilt etc." },
        new() { DuHarLas = "5041", Kategori = "Ving", Laskasse = "9788", Utskiftningsskilt = "7606", Sylinder = "65537/5537/1237" },
        new() { DuHarLas = "5051", Kategori = "Ving", Laskasse = "9788", Utskiftningsskilt = "7606", Sylinder = "65537/5537/1237" },
        new() { DuHarLas = "5062", Kategori = "Ving", Laskasse = "5585", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5045", Kategori = "Ving", Laskasse = "9787", Utskiftningsskilt = "7606", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5043", Kategori = "Ving", Laskasse = "5049/25-35", Utskiftningsskilt = "5974", Sylinder = "65520/5520/1220" },
        new() { DuHarLas = "31", Kategori = "Ving", Laskasse = "22501", Sylinder = "-" },

        // TrioVing smalprofil
        new() { DuHarLas = "5017/25-30", Kategori = "TrioVing smalprofil", Laskasse = "5016/25", Utskiftningsskilt = "7607/7649", Sylinder = "65520/5520/1220", Annet = "5976 langskilt" },
        new() { DuHarLas = "5036(25-30)/5035", Kategori = "TrioVing smalprofil", Laskasse = "5032(25-35)", Utskiftningsskilt = "7608/7646", Sylinder = "5527", Annet = "5927 stolpeforlenger" },
        new() { DuHarLas = "5036(25-30)/5035/5034", Kategori = "TrioVing smalprofil", Laskasse = "5032(25-35)", Utskiftningsskilt = "7645", Sylinder = "65520/5520/1220", Annet = "5974 langskilt, 5927 stolpeforlenger" },
        new() { DuHarLas = "5046/25-30", Kategori = "TrioVing smalprofil", Laskasse = "5049(25-35)", Utskiftningsskilt = "7649", Sylinder = "5520C/1220C", Annet = "5974 langskilt" },

        // TrioVing (51-serie til modullås)
        new() { DuHarLas = "5116", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "565", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5119", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "8765", Sylinder = "5596C" },
        new() { DuHarLas = "5596", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "2002", Utskiftningsskilt = "SK4318", Sylinder = "5585C" },
        new() { DuHarLas = "5122", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "562", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5130", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "1498", Sylinder = "Kun vrider" },
        new() { DuHarLas = "5132", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "5585", Sylinder = "45/5545/1245" },
        new() { DuHarLas = "5133", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "8561", Sylinder = "5545C/1245C" },
        new() { DuHarLas = "5135", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "8768", Sylinder = "5545C/1245C" },
        new() { DuHarLas = "5141", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "411", Sylinder = "5585C/1285C", Annet = "sluttstykke 1887-1" },
        new() { DuHarLas = "5149", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "411", Sylinder = "5585C/1285C", Annet = "sluttstykke 1887-1" },
        new() { DuHarLas = "5181", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "2587", Sylinder = "45/5545/1245", Annet = "sluttstykke 1887-1", Alternativt = "311" },
        new() { DuHarLas = "5075", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "5775*", Merknad = "5775 er inkl. sylindersett." },
        new() { DuHarLas = "5111", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "2002", Utskiftningsskilt = "8659", Sylinder = "5585" },
        new() { DuHarLas = "5112", Kategori = "TrioVing (51-serie → modullås)", Laskasse = "5116", Sylinder = "5520/1220" },

        // TrioVing smalprofil (modullås)
        new() { DuHarLas = "441", Kategori = "TrioVing smalprofil (modullås)", Laskasse = "2002", Utskiftningsskilt = "SK4318", Sylinder = "5585C" },
        new() { DuHarLas = "445", Kategori = "TrioVing smalprofil (modullås)", Laskasse = "8765", Sylinder = "65537/5537/1237" },
        new() { DuHarLas = "4160", Kategori = "TrioVing smalprofil (modullås)", Laskasse = "2016", Sylinder = "65520/5520/1220" },
        new() { DuHarLas = "LC200", Kategori = "TrioVing smalprofil (modullås)", Laskasse = "8765", Sylinder = "65537/5537/1237" },
        new() { DuHarLas = "4565", Kategori = "TrioVing smalprofil (modullås)", Laskasse = "565/8765", Sylinder = "65537/5537/1237" },
        new() { DuHarLas = "4865", Kategori = "TrioVing smalprofil (modullås)", Laskasse = "8765", Sylinder = "65537/5537/1237" },
    ];

    public static string? MerknadForKategori(string kategori) => kategori switch
    {
        "Mellomdørlåser" => MerknadMellomdorlaser,
        "Trio" => MerknadTrio,
        "TrioVing (51-serie → modullås)" or "TrioVing smalprofil (modullås)" => MerknadModullas,
        _ => null
    };

    public static List<LasOvergangRad> Sok(string tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return [];
        }

        return Rader
            .Where(r => r.DuHarLas.Contains(tekst, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.DuHarLas.Length)
            .ThenBy(r => r.DuHarLas)
            .ToList();
    }
}
