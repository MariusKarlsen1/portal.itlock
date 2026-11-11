namespace PortalItlock.Web.Models;

// Snarveier til viktige ressurser på forsiden (Tripletex, leverandørportaler osv.).
// Admin legger til egne med navn, lenke og logo. Standardsnarveiene bruker en
// innebygd logo fra wwwroot/img/snarveier (StandardLogo) til en egen er lastet opp.
public class Snarvei
{
    public int Id { get; set; }
    public required string Navn { get; set; }
    public required string Url { get; set; }
    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public string? StandardLogo { get; set; }
    public int Rekkefolge { get; set; }
    public DateTime Endret { get; set; } = DateTime.Now;
}
