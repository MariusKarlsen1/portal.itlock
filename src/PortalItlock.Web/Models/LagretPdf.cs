namespace PortalItlock.Web.Models;

public class LagretPdf
{
    public int Id { get; set; }
    public required string EntityType { get; set; }
    public required int EntityId { get; set; }

    public required string Navn { get; set; }
    public required byte[] Data { get; set; }

    /// Serialisert List&lt;LagretPdfLinje&gt; - brukes til å sammenligne to lagrede versjoner (utstyr/pris).
    public string? DataJson { get; set; }

    /// Serialisert List&lt;LagretPdfNokkeltall&gt; - overordnede tall (f.eks. estimert timer, totalsum) for sammenligning.
    public string? NokkeltallJson { get; set; }

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}

public record LagretPdfLinje(string Navn, int Antall, decimal? Pris);

/// Tall/Enhet brukes til å beregne og vise differansen ved sammenligning (f.eks. "+1,2 t" eller "−1 746 kr").
public record LagretPdfNokkeltall(string Navn, string Verdi, decimal Tall = 0, string Enhet = "");
