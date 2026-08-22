using SkiaSharp;

namespace PortalItlock.Web.Services;

public static class PdfBilde
{
    public static byte[] Forminsk(byte[] data, int maxBredde = 800)
    {
        using var bitmap = SKBitmap.Decode(data);
        if (bitmap is null || bitmap.Width <= maxBredde)
        {
            return data;
        }

        var hoyde = Math.Max(1, (int)Math.Round(bitmap.Height * (maxBredde / (double)bitmap.Width)));
        using var resized = bitmap.Resize(new SKImageInfo(maxBredde, hoyde), SKSamplingOptions.Default);
        if (resized is null)
        {
            return data;
        }

        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
