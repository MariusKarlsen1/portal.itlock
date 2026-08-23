using System.Text.RegularExpressions;

namespace PortalItlock.Web.Services;

public static class ByggetrinnHelper
{
    // Naturlig sortering slik at "BT2" kommer før "BT10".
    public static IEnumerable<string> Sorter(IEnumerable<string> byggetrinn) =>
        byggetrinn.OrderBy(b => Regex.Replace(b, @"\d+", m => m.Value.PadLeft(6, '0')));
}
