namespace PortalItlock.Web.Models;

public enum HjemLayout
{
    Klassisk,
    Bilde,
    Dashboard3,

    // Layout 3: forside med fjordbilde og kort (HjemNy.razor). Lagt til sist,
    // så tallverdiene til de eksisterende valgene ikke endres.
    Fjord
}
