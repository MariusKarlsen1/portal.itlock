using System.Text.RegularExpressions;

namespace PortalItlock.Web.Services;

public static class ByggetrinnHelper
{
    // Naturlig sortering slik at "BT2" kommer før "BT10".
    public static IEnumerable<string> Sorter(IEnumerable<string> byggetrinn) =>
        byggetrinn.OrderBy(b => Regex.Replace(b, @"\d+", m => m.Value.PadLeft(6, '0')));

    // Parser et kommaseparert byggetrinn-filter (fra query-param eller lagret felt) til en liste, eller null hvis "alle".
    public static List<string>? ParseFilter(string? filter) =>
        string.IsNullOrWhiteSpace(filter)
            ? null
            : filter.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
}
