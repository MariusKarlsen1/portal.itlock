namespace PortalItlock.Web.Models;

/// <summary>
/// Markerer at en nøkkel åpner en gitt reservesylinder (LasplanReserve). Ett kryss i låsplan-matrisen.
/// </summary>
public class NokkelLasplanReserve
{
    public int NokkelId { get; set; }
    public Nokkel? Nokkel { get; set; }

    public int LasplanReserveId { get; set; }
    public LasplanReserve? LasplanReserve { get; set; }
}
