using SkiaSharp;

namespace PortalItlock.Web.Services;

// Lager små miniatyrbilder til lister (f.eks. kundelogoer i liste- og
// sidepanelvisning), slik at nettleseren ikke laster ned originalbildet
// i full størrelse bare for å vise det i 32 px.
public static class BildeMiniatyr
{
    public static (byte[] Data, string ContentType)? Lag(byte[] data, int maksSide)
    {
        // SKBitmap.Decode kaster på data den ikke kjenner igjen - da sendes originalen.
        using var stream = new SKMemoryStream(data);
        using var codec = SKCodec.Create(stream);
        if (codec is null)
        {
            return null;
        }

        using var bitmap = SKBitmap.Decode(codec);
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

        // Mobilbilder er ofte lagret liggende med en EXIF-retning. Nettleseren
        // snur originalen selv, men miniatyren mister EXIF og må snus her.
        using var rettet = Roter(forminsket, codec.EncodedOrigin);

        // WebP beholder gjennomsiktighet i logoer og er mye mindre enn PNG.
        using var image = SKImage.FromBitmap(rettet ?? forminsket);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 85);
        return encoded is null ? null : (encoded.ToArray(), "image/webp");
    }

    private static SKBitmap? Roter(SKBitmap kilde, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft)
        {
            return null;
        }

        var bytterSider = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var b = bytterSider ? kilde.Height : kilde.Width;
        var h = bytterSider ? kilde.Width : kilde.Height;
        var resultat = new SKBitmap(b, h, kilde.ColorType, kilde.AlphaType);
        using var canvas = new SKCanvas(resultat);
        canvas.SetMatrix(origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, b, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, b, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, b, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, b, -1, 0, h, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, h, 0, 0, 1),
            _ => SKMatrix.Identity
        });
        canvas.DrawBitmap(kilde, 0, 0);
        return resultat;
    }
}
