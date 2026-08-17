namespace PortalItlock.Web.Models;

public class PrisoverslagLinje
{
    public int Id { get; set; }
    public int PrisoverslagId { get; set; }
    public Prisoverslag? Prisoverslag { get; set; }

    public required string Navn { get; set; }
    public decimal PrisNetto { get; set; }
    public decimal PrisVeiledende { get; set; }
    public int Antall { get; set; } = 1;
}
