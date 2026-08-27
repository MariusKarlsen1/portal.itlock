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

    public DateTime OpprettetDato { get; set; } = DateTime.Now;
}

public record LagretPdfLinje(string Navn, int Antall, decimal? Pris);
