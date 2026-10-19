using SkiaSharp;

namespace PortalItlock.Web.Services;

// Lager små miniatyrbilder til lister (f.eks. kundelogoer i liste- og
// sidepanelvisning), slik at nettleseren ikke laster ned originalbildet
// i full størrelse bare for å vise det i 32 px.
public static class BildeMiniatyr
{
    // trimKanter: skjærer bort tomme kanter (hvit eller gjennomsiktig bakgrunn)
    // rundt motivet, så varen fyller rammen i stedet for å bli en liten prikk
    // midt i et stort hvitt produktbilde fra leverandøren.
    public static (byte[] Data, string ContentType)? Lag(byte[] data, int maksSide, bool trimKanter = false)
    {
        // SKBitmap.Decode kaster på data den ikke kjenner igjen - da sendes originalen.
        using var stream = new SKMemoryStream(data);
        using var codec = SKCodec.Create(stream);
        if (codec is null)
        {
            return null;
        }

        using var dekodet = SKBitmap.Decode(codec);
        if (dekodet is null)
        {
            return null;
        }

        using var trimmet = trimKanter ? Trim(dekodet) : null;
        var bitmap = trimmet ?? dekodet;

        var storst = Math.Max(bitmap.Width, bitmap.Height);
        if (storst <= maksSide && trimmet is null)
        {
            return null;
        }

        using var forminsket = storst <= maksSide ? null : Forminsk(bitmap, maksSide / (double)storst);
        var kilde = forminsket ?? bitmap;

        // Mobilbilder er ofte lagret liggende med en EXIF-retning. Nettleseren
        // snur originalen selv, men miniatyren mister EXIF og må snus her.
        using var rettet = Roter(kilde, codec.EncodedOrigin);

        // WebP beholder gjennomsiktighet i logoer og er mye mindre enn PNG.
        using var image = SKImage.FromBitmap(rettet ?? kilde);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 85);
        return encoded is null ? null : (encoded.ToArray(), "image/webp");
    }

    private static SKBitmap Forminsk(SKBitmap kilde, double skala)
    {
        var bredde = Math.Max(1, (int)Math.Round(kilde.Width * skala));
        var hoyde = Math.Max(1, (int)Math.Round(kilde.Height * skala));
        return kilde.Resize(new SKImageInfo(bredde, hoyde), new SKSamplingOptions(SKCubicResampler.Mitchell)) ?? kilde.Copy();
    }

    // Finner motivet ved å sammenligne med fargen i hjørnene (bakgrunnen) og
    // skjærer bort kantene rundt, med litt luft igjen. Gir null når det ikke er
    // noe å skjære bort (f.eks. et foto uten ensfarget bakgrunn).
    private static SKBitmap? Trim(SKBitmap kilde)
    {
        using var rgba = kilde.ColorType == SKColorType.Rgba8888 ? null : kilde.Copy(SKColorType.Rgba8888);
        var b = rgba ?? kilde;
        if (b.Width < 8 || b.Height < 8)
        {
            return null;
        }

        var px = b.GetPixelSpan();
        var w = b.Width;
        var h = b.Height;
        var rad = b.RowBytes;

        (byte R, byte G, byte B, byte A) Hent(ReadOnlySpan<byte> p, int x, int y)
        {
            var i = y * rad + x * 4;
            return (p[i], p[i + 1], p[i + 2], p[i + 3]);
        }

        var hjorner = new[] { Hent(px, 0, 0), Hent(px, w - 1, 0), Hent(px, 0, h - 1), Hent(px, w - 1, h - 1) };
        var bg = hjorner[0];
        const int toleranse = 28;

        bool Lik((byte R, byte G, byte B, byte A) a, (byte R, byte G, byte B, byte A) c) =>
            (a.A < 16 && c.A < 16) ||
            (Math.Abs(a.A - c.A) <= toleranse && Math.Abs(a.R - c.R) <= toleranse && Math.Abs(a.G - c.G) <= toleranse && Math.Abs(a.B - c.B) <= toleranse);

        // Bare når alle fire hjørnene har samme bakgrunn - ellers er det et vanlig foto.
        if (hjorner.Any(c => !Lik(c, bg)))
        {
            return null;
        }

        int venstre = w, hoyre = -1, topp = h, bunn = -1;
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                if (Lik(Hent(px, x, y), bg))
                {
                    continue;
                }

                if (x < venstre) venstre = x;
                if (x > hoyre) hoyre = x;
                if (y < topp) topp = y;
                if (y > bunn) bunn = y;
            }
        }

        if (hoyre < 0)
        {
            return null;
        }

        // Litt luft rundt motivet (4 % av største side).
        var luft = (int)Math.Round(Math.Max(hoyre - venstre, bunn - topp) * 0.04);
        venstre = Math.Max(0, venstre - luft);
        topp = Math.Max(0, topp - luft);
        hoyre = Math.Min(w - 1, hoyre + luft);
        bunn = Math.Min(h - 1, bunn + luft);

        var nyBredde = hoyre - venstre + 1;
        var nyHoyde = bunn - topp + 1;

        // Ikke verdt det når kantene er nesten borte allerede.
        if (nyBredde * (long)nyHoyde > w * (long)h * 0.92)
        {
            return null;
        }

        using var utsnitt = new SKBitmap();
        return kilde.ExtractSubset(utsnitt, new SKRectI(venstre, topp, hoyre + 1, bunn + 1)) ? utsnitt.Copy() : null;
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
