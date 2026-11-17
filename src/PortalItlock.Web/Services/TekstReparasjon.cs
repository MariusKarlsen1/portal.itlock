using System.Text;

namespace PortalItlock.Web.Services;

// Filnavn fra zip/Windows kommer av og til med feil tegnsett: «Tømrerplan» blir
// «T├©mrerplan» (UTF-8 lest som CP850) eller «TÃ¸mrerplan» (UTF-8 lest som Latin-1).
public static class TekstReparasjon
{
    private static readonly Encoding Cp850;

    static TekstReparasjon()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp850 = Encoding.GetEncoding(850);
    }

    public static string Fiks(string? tekst)
    {
        if (string.IsNullOrEmpty(tekst))
        {
            return tekst ?? "";
        }
        if (tekst.IndexOfAny(['├', '┬', '╞', '╪']) >= 0 && Prov(Cp850, tekst) is { } a)
        {
            return a;
        }
        if (tekst.IndexOfAny(['Ã', 'Â']) >= 0 && Prov(Encoding.Latin1, tekst) is { } b)
        {
            return b;
        }
        return tekst;
    }

    private static string? Prov(Encoding feil, string tekst)
    {
        var bytes = feil.GetBytes(tekst);
        var utf8 = new UTF8Encoding(false, true);
        try
        {
            var ut = utf8.GetString(bytes);
            return ut.Contains('?') && !tekst.Contains('?') ? null : ut;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }
}
