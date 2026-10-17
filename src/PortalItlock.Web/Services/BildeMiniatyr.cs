using SkiaSharp;

namespace PortalItlock.Web.Services;

// Lager små miniatyrbilder til lister (f.eks. kundelogoer i liste- og
// sidepanelvisning), slik at nettleseren ikke laster ned originalbildet
// i full størrelse bare for å vise det i 32 px.
public static class BildeMiniatyr
{
    public static (byte[] Data, string ContentType)? Lag(byte[] data, int maksSide)
    {
        using var bitmap = SKBitmap.Decode(data);
        if (bitmap is null)
        {
            return null;
        }

        var storst = Math.Max(bitmap.Width, bitmap.Height);
        if (storst <= maksSide)
        {
            return null;
        }

        var skala = maksSide / (double)storst;
        var bredde = Math.Max(1, (int)Math.Round(bitmap.Width * skala));
        var hoyde = Math.Max(1, (int)Math.Round(bitmap.Height * skala));
        using var forminsket = bitmap.Resize(new SKImageInfo(bredde, hoyde), new SKSamplingOptions(SKCubicResampler.Mitchell));
        if (forminsket is null)
        {
            return null;
        }

        // WebP beholder gjennomsiktighet i logoer og er mye mindre enn PNG.
        using var image = SKImage.FromBitmap(forminsket);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 85);
        return encoded is null ? null : (encoded.ToArray(), "image/webp");
    }
}
