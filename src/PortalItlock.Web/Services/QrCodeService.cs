using QRCoder;

namespace PortalItlock.Web.Services;

public static class QrCodeService
{
    public static byte[] GeneratePng(string innhold, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(innhold, QRCodeGenerator.ECCLevel.M);
        var renderer = new PngByteQRCode(data);
        return renderer.GetGraphic(pixelsPerModule);
    }
}
