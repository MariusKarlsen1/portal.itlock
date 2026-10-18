namespace PortalItlock.Web.Models;

// Bildet til en kunde. Ligger i samme tabell (Kunder), men som egen entitet
// (table splitting) slik at det bare lastes der det brukes - se Kunde.Bilde.
public class KundeBilde
{
    public int Id { get; set; }
    public byte[]? BildeData { get; set; }
}