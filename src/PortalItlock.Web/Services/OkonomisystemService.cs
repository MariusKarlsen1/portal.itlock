using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Data;
using PortalItlock.Web.Models;

namespace PortalItlock.Web.Services;

// Hvilket økonomisystem organisasjonen fakturerer i, og hva det betyr for
// sidene. Sidene spør denne i stedet for å nevne Tripletex direkte:
//  - Navn: «Tripletex», eller navnet på eget system («Fiken») / «økonomisystemet»
//  - KanSende: ordre og akontoer sendes automatisk (i dag bare Tripletex)
//  - ErManuell: fakturaen lages for hånd, og portalen spør om det er fakturert
// PowerOffice og Visma legges til her når koblingene er klare.
public sealed class OkonomisystemService(PlatformDbContext platformDb, ITenantContext tenant, TripletexService tripletex)
{
    private bool _lest;
    private OkonomisystemType? _valgt;
    private string? _egetNavn;

    private void Les()
    {
        if (_lest)
        {
            return;
        }

        _lest = true;
        if (tenant.Current is not { } t)
        {
            return;
        }

        var inn = platformDb.TenantInnstillinger.AsNoTracking()
            .Where(i => i.TenantId == t.Id)
            .Select(i => new { i.Okonomisystem, i.OkonomisystemNavn })
            .FirstOrDefault();
        _valgt = inn?.Okonomisystem;
        _egetNavn = string.IsNullOrWhiteSpace(inn?.OkonomisystemNavn) ? null : inn!.OkonomisystemNavn!.Trim();
    }

    public OkonomisystemType Type
    {
        get
        {
            Les();
            return _valgt ?? (tripletex.ErKonfigurert ? OkonomisystemType.Tripletex : OkonomisystemType.Ingen);
        }
    }

    // Ordre og akontoer kan sendes rett til systemet.
    public bool KanSende => Type == OkonomisystemType.Tripletex && tripletex.ErKonfigurert;

    public bool ErManuell => !KanSende;

    // Kunder og varer kan hentes/synkes fra systemet.
    public bool KanSynke => KanSende;

    public string Navn
    {
        get
        {
            Les();
            return Type switch
            {
                OkonomisystemType.Tripletex => "Tripletex",
                OkonomisystemType.PowerOffice => "PowerOffice",
                OkonomisystemType.Visma => "Visma",
                _ => _egetNavn ?? "økonomisystemet"
            };
        }
    }

    // «i Tripletex», «i Fiken», «i økonomisystemet».
    public string INavn => "i " + Navn;

    public static string Visningsnavn(OkonomisystemType t) => t switch
    {
        OkonomisystemType.Tripletex => "Tripletex",
        OkonomisystemType.PowerOffice => "PowerOffice Go",
        OkonomisystemType.Visma => "Visma",
        _ => "Ingen, fakturerer selv"
    };
}
