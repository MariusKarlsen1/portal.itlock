namespace PortalItlock.Web.Models;

/// <summary>
/// En sylinder som hører til prosjektets låsplan, men som ikke (ennå) er montert på noen dør -
/// f.eks. en reservesylinder som skal leveres sammen med resten av bestillingen.
/// </summary>
public class LasplanReserve
{
    public int Id { get; set; }

    public int ProsjektId { get; set; }
    public Prosjekt? Prosjekt { get; set; }

    public int ComponentId { get; set; }
    public Component? Component { get; set; }

    public int Antall { get; set; } = 1;
    public string? Notat { get; set; }

    // Fritekst - dør(er) sylinderen er tiltenkt, selv om den ikke (ennå) er lagt til på en faktisk dør.
    public string? DorTil { get; set; }
    public string? Dornr { get; set; }
}
