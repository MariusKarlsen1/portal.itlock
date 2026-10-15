namespace PortalItlock.Web.Models;

// Filene til en vare (Component). Ligger i samme tabell (Components), men som
// egen entitet (table splitting) slik at de bare lastes med
// .Include(c => c.Filer) der de faktisk brukes - se Component.Filer.
public class ComponentFiler
{
    public int Id { get; set; }
    public byte[]? FdvData { get; set; }
    public byte[]? MontasjebladData { get; set; }
    public byte[]? DatabladData { get; set; }
    public byte[]? BildeData { get; set; }
}
