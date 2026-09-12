namespace PortalItlock.Web.Services;

// Lar en side (f.eks. Prosjekter) registrere en filter-handling som det
// faste toppfeltet på mobil (MainLayout) kan vise et filterikon for og
// utløse - uten at layoutet trenger å kjenne til den enkelte sidens
// filter-tilstand. Scoped per krets, så toppfeltet og siden deler samme
// instans gjennom hele sesjonen.
public class MobilVerktoylinjeService
{
    public event Action? Endret;

    private Action? _filterToggle;
    private Func<bool>? _filterErAktivt;

    public bool HarFilter => _filterToggle is not null;
    public bool FilterErAktivt => _filterErAktivt?.Invoke() ?? false;

    public void RegistrerFilter(Action toggle, Func<bool> erAktivt)
    {
        _filterToggle = toggle;
        _filterErAktivt = erAktivt;
        Endret?.Invoke();
    }

    public void AvregistrerFilter()
    {
        _filterToggle = null;
        _filterErAktivt = null;
        Endret?.Invoke();
    }

    public void UtlosFilter() => _filterToggle?.Invoke();

    // Kalles av siden selv når filterstatus endrer seg internt (f.eks. et
    // filter fjernes), slik at toppfeltets aktiv-indikator holdes i sync.
    public void VarsleEndring() => Endret?.Invoke();
}
