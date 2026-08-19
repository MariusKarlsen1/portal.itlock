using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace PortalItlock.Web.Services;

/// <summary>
/// Rendrer den faktiske itlock-logoen (wwwroot/itlock-logo.svg) i genererte PDF-er,
/// i stedet for en håndtegnet tekst-etterligning.
/// </summary>
public class PdfLogo(IWebHostEnvironment env)
{
    private readonly Lazy<string> _svgInnhold = new(() =>
        File.ReadAllText(Path.Combine(env.WebRootPath, "itlock-logo.svg")));

    public void Render(IContainer container, float height)
    {
        container.Height(height).Svg(_svgInnhold.Value).FitHeight();
    }
}
