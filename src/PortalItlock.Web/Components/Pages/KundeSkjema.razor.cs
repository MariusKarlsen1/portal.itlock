using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using PortalItlock.Web.Models;
using PortalItlock.Web.Services;

namespace PortalItlock.Web.Components.Pages;

// Logikken bak kundekortet (K5 · faner på siden). Markupen ligger i KundeSkjema.razor.
public partial class KundeSkjema
{
    [Parameter] public int Id { get; set; }
    [SupplyParameterFromQuery(Name = "fane")] public string? Fane { get; set; }
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }

    private enum KundeFane { Oversikt, Oppfolging, Prosjekter, Tilbud, Ordrer, Tickets, Avtaler, Priser, Kontaktpersoner, Detaljer }

    private sealed class ProsjektRad
    {
        public int Id { get; set; }
        public string Navn { get; set; } = "";
        public string? Adresse { get; set; }
        public string? System { get; set; }
        public ProsjektStatus? Status { get; set; }
        public string? Ansvarlig { get; set; }
        public int Dorer { get; set; }
        public int Ferdige { get; set; }
    }

    private sealed record ServiceavtaleRad(int Id, string Navn, int Dorer, DateTime? SisteRunde, DateTime? NesteService);
    private sealed record AktivitetRad(string Emoji, string Bakgrunn, string Tittel, string? Tekst, string? Hvem, DateTime Dato, string? Lenke);
    private sealed record UtfortRad(DateTime Dato, string Emoji, string Tittel, string? Hvem, string? Tekst, bool ErNotat);

    private const int TilbudPerSide = 10;
    private static readonly CultureInfo Kultur = new("nb-NO");
    private static readonly (string Tekst, int Dager)[] Snarvalg = [("I morgen", 1), ("Om 1 uke", 7), ("Om 2 uker", 14), ("Om 1 måned", 30), ("Om 3 måneder", 91)];
    private static readonly (string Tekst, int Dager)[] VarselNar = [("På dagen kl. 08:00", 0), ("1 dag før", 1), ("1 uke før", 7)];
    private static readonly string[] AvatarFarger = ["#7a2b96", "#2054a3", "#2b7a4b", "#835e41", "#b0322a", "#9a5b12"];

    private Kunde? _kunde;
    private bool _notFound;
    private int? _loadedId;
    private KundeFane _fane = KundeFane.Oversikt;
    private int? _naverendeBrukerId;
    private Bruker? _naverendeBruker;

    private bool _confirmingDelete;
    private string? _error;
    private string? _savedMessage;
    private string? _bildeFeil;
    private int _bildeVersjon;
    private bool _slarOppOrgNr;
    private string? _orgNrFeil;
    private string? _brregMelding;
    private List<BronnoysundClient.Sokeresultat> _navnForslag = [];
    private bool _visNavnForslag;
    private bool _sokerNavn;
    private int _sokGenerasjon;

    private bool _visNyttMeny;
    private bool _visNyttTilbud;
    private bool _redigerFirma;
    private bool _redigerFaktura;

    private List<ProsjektRad> _prosjekter = [];
    private string _prosjektFilter = "alle";
    private List<Arbeidsordre> _arbeidsordrer = [];
    private string _ordreFilter = "Alle";
    private List<Tilbud> _tilbudListe = [];
    private string _tilbudFilter = "Alle";
    private int _tilbudAr = DateTime.Today.Year;
    private int _tilbudSide = 1;
    private List<Ticket> _tickets = [];
    private string _ticketFilter = "apne";
    private bool _visNyTicket;
    private string _nyTicketTittel = "";
    private decimal _fakturertIAr;
    private decimal _fakturertIFjor;

    private List<KundeOppfolgingNotat> _notater = [];
    private string _nyttNotat = "";
    private bool _visLogg;
    private List<KundeOppfolging> _oppfolginger = [];
    private List<Bruker> _ansatte = [];
    private int? _ofRedigerId;
    private string _ofTittel = "";
    private KundeOppfolgingType _ofType = KundeOppfolgingType.Ring;
    private DateOnly? _ofDato;
    private TimeOnly _ofTid = new(9, 0);
    private int? _ofAnsvarligId;
    private string? _ofKontakt;
    private bool _ofPortal = true;
    private bool _ofEpost = true;
    private int _ofDagerFor;
    private KundeOppfolgingGjenta _ofGjenta = KundeOppfolgingGjenta.Ikke;
    private string? _ofNotat;
    private string? _ofFeil;
    private string? _ofMelding;

    private List<KundeKontaktperson> _kontaktpersoner = [];
    private string _nyKontaktpersonNavn = "";
    private bool _visNyKontakt;
    private int? _redigerKontaktId;

    private List<ServiceavtaleRad> _serviceavtaler = [];
    private List<Bruker> _portalbrukere = [];
    private List<KundeLisens> _kundeLisenser = [];
    private bool _visNyLisens;
    private string _nyLisensNavn = "";
    private DateTime _nyLisensUtlopsDato = DateTime.Today.AddYears(1);
    private string? _lisensFeil;
    private string? _lisensMelding;

    private List<Produktgruppe> _produktgrupper = [];
    private List<KundeRabatt> _kundeRabatter = [];
    private bool _visNyRabatt;
    private bool _visKopier;
    private int? _nyRabattProduktgruppeId;
    private decimal _nyRabattProsent;
    private string? _rabattFeil;
    private string? _rabattMelding;
    private string? _paslagMelding;
    private List<Kunde> _andreKunder = [];
    private int? _kopierTilKundeId;

    // ---------------- Avledet ----------------

    private string? KundeAdresse => _kunde is null ? null : string.Join(", ", new[] { _kunde.Adresse, $"{_kunde.Postnr} {_kunde.Sted}".Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
    private List<KundeOppfolging> ApneOppfolginger => _oppfolginger.Where(o => !o.Utfort).OrderBy(o => o.Frist).ToList();
    private KundeOppfolging? NesteOppfolging => ApneOppfolginger.FirstOrDefault();
    private DateTime? NesteFrist => NesteOppfolging?.Frist ?? _kunde?.NesteOppfolgingsDato;
    private List<Tilbud> VenterSvar => _tilbudListe.Where(t => t.Status == EndringsmeldingStatus.SendtTilKunde).ToList();
    private List<Ticket> ApneTickets => _tickets.Where(t => t.Status != TicketStatus.Lukket).ToList();
    private bool LisensNaermerSeg => _kundeLisenser.Any(l => l.UtlopsDato.Date <= DateTime.Today.Add(LisensVarselService.VarselFrist));
    private int AntallKontakter => _kontaktpersoner.Count + (string.IsNullOrWhiteSpace(_kunde?.Kontaktperson) ? 0 : 1);
    private string? AnsvarligEpost => _ansatte.FirstOrDefault(b => b.Id == _ofAnsvarligId)?.Epost;

    private List<string> Kontaktnavn => new[] { _kunde?.Kontaktperson }
        .Concat(_kontaktpersoner.Select(k => (string?)k.Navn))
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .Select(n => n!)
        .Distinct()
        .ToList();

    private int? EldsteVenter => VenterSvar.Count == 0 ? null
        : VenterSvar.Max(t => (DateTime.Today - (t.SendtDato ?? t.OpprettetDato.ToLocalTime()).Date).Days);

    private string FakturertEndring
    {
        get
        {
            if (_fakturertIFjor <= 0)
            {
                return _fakturertIAr > 0 ? "første år med fakturering" : "ingen fakturert ennå";
            }
            var endring = (int)Math.Round((_fakturertIAr - _fakturertIFjor) / _fakturertIFjor * 100);
            return endring >= 0 ? $"▲ {endring} % mot i fjor" : $"▼ {-endring} % mot i fjor";
        }
    }

    private List<AktivitetRad> Aktivitet
    {
        get
        {
            var liste = new List<AktivitetRad>();
            liste.AddRange(_oppfolginger.Where(o => o.Utfort && o.UtfortDato.HasValue).Select(o =>
                new AktivitetRad(o.Type.Emoji(), o.Type == KundeOppfolgingType.Ring ? "var(--k-bla-l)" : "var(--k-gul-l)", o.Tittel, o.UtfortKommentar ?? o.Notat, o.UtfortAvBruker?.Navn, o.UtfortDato!.Value, null)));
            liste.AddRange(_notater.Select(n =>
                new AktivitetRad("📝", "var(--k-gra-l)", "Kontakt logget", n.Tekst.Length > 140 ? n.Tekst[..140] + "…" : n.Tekst, n.OpprettetAvBruker?.Navn, n.OpprettetDato, null)));
            liste.AddRange(_tickets.Select(t =>
                new AktivitetRad("🎫", "var(--k-rod-l)", $"Ticket #{t.Id} – {t.Tittel}", TicketSted(t) == "–" ? null : TicketSted(t), t.Kontaktperson, t.OpprettetDato.ToLocalTime(), $"/ticket/{t.Id}")));
            liste.AddRange(_tilbudListe.Select(t => t.SendtDato is { } sendt
                ? new AktivitetRad("📄", "var(--k-gul-l)", $"Tilbud sendt – {t.Tittel}", string.Join(" · ", new[] { t.SendtSum is { } s ? $"{Kr(s)} eks. mva" : null, t.GyldigTil is { } g ? $"gyldig til {g:d. MMM}" : null }.Where(x => x is not null)), null, sendt, $"/tilbud/{t.Id}")
                : new AktivitetRad("📄", "var(--k-gul-l)", $"Tilbud opprettet – {t.Tittel}", t.Prosjekt?.Navn, null, t.OpprettetDato.ToLocalTime(), $"/tilbud/{t.Id}")));
            liste.AddRange(_arbeidsordrer.Select(a => a.FakturertDato is { } fd
                ? new AktivitetRad("🧾", "var(--k-ok-l)", $"AO-{a.Id:0000} fakturert", a.Tittel, null, fd, $"/arbeidsordre/{a.Id}")
                : new AktivitetRad("🔧", "var(--k-bla-l)", $"Arbeidsordre AO-{a.Id:0000} – {a.Tittel}", $"{OrdreStatusTekst(a.Status)}{(a.Prosjekt is null ? "" : $" · {a.Prosjekt.Navn}")}", a.AnsvarligMontor?.Navn, a.OpprettetDato.ToLocalTime(), $"/arbeidsordre/{a.Id}")));
            return liste.OrderByDescending(a => a.Dato).Take(8).ToList();
        }
    }

    private List<UtfortRad> UtfortLogg =>
        _oppfolginger.Where(o => o.Utfort).Select(o => new UtfortRad(o.UtfortDato ?? o.Frist, o.Type.Emoji(), o.Tittel, $"Utført av {o.UtfortAvBruker?.Navn ?? "ukjent"}", o.UtfortKommentar, false))
            .Concat(_notater.Select(n => new UtfortRad(n.OpprettetDato, "📝", n.Tekst.Length > 80 ? n.Tekst[..80] + "…" : n.Tekst, n.OpprettetAvBruker?.Navn ?? "", null, true)))
            .OrderByDescending(u => u.Dato)
            .ToList();

    private string ForhandsvisVarselDato
    {
        get
        {
            var frist = (_ofDato ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(_ofTid);
            return Stor(frist.AddDays(-_ofDagerFor).ToString("dddd d. MMM", Kultur)) + " 08:00";
        }
    }

    private string ForhandsvisFrist
    {
        get
        {
            var frist = (_ofDato ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(_ofTid);
            return _ofDagerFor == 0 ? $"i dag {frist:HH:mm}" : $"{frist.ToString("dddd d. MMM", Kultur)} {frist:HH:mm}";
        }
    }

    // ---------------- Prosjekter ----------------

    private static string ProsjektGruppe(ProsjektStatus? s) => s switch
    {
        ProsjektStatus.Aktiv or ProsjektStatus.UnderArbeid or ProsjektStatus.Registrert => "pagar",
        ProsjektStatus.Tilbud => "tilbud",
        ProsjektStatus.Overlevert or ProsjektStatus.Avsluttet or ProsjektStatus.TilbudAvslatt => "ferdig",
        ProsjektStatus.Serviceavtale => "service",
        _ => "annet"
    };

    private IEnumerable<ProsjektRad> FiltrerteProsjekter => _prosjektFilter == "alle" ? _prosjekter : _prosjekter.Where(p => ProsjektGruppe(p.Status) == _prosjektFilter);

    private static string ProsjektKortStatus(ProsjektStatus? s) => s switch
    {
        ProsjektStatus.Aktiv or ProsjektStatus.UnderArbeid => "Pågår",
        ProsjektStatus.Registrert => "Registrert",
        ProsjektStatus.Tilbud => "Tilbud sendt",
        ProsjektStatus.TilbudAvslatt => "Tilbud avslått",
        ProsjektStatus.Overlevert => "Ferdig",
        ProsjektStatus.Avsluttet => "Avsluttet",
        ProsjektStatus.Serviceavtale => "Serviceavtale",
        _ => "Uten status"
    };

    private static string ProsjektStatusTekst(ProsjektRad p) =>
        ProsjektGruppe(p.Status) == "pagar" && p.Dorer > 0 && p.Status != ProsjektStatus.Registrert
            ? $"Pågår · {p.Ferdige * 100 / p.Dorer} %"
            : ProsjektKortStatus(p.Status);

    private static string ProsjektPillKlasse(ProsjektStatus? s) => s switch
    {
        ProsjektStatus.Aktiv or ProsjektStatus.UnderArbeid => "s-bla",
        ProsjektStatus.Tilbud => "s-gul",
        ProsjektStatus.Overlevert or ProsjektStatus.Avsluttet => "s-ok",
        ProsjektStatus.TilbudAvslatt => "s-rod",
        ProsjektStatus.Serviceavtale => "s-lilla",
        _ => "s-gra"
    };

    private static string ProsjektBakgrunn(ProsjektStatus? s) => ProsjektGruppe(s) switch
    {
        "pagar" => "var(--k-bla-l)",
        "tilbud" => "var(--k-gul-l)",
        "ferdig" => "var(--k-ok-l)",
        "service" => "var(--k-lilla-l)",
        _ => "var(--k-gra-l)"
    };

    private static string ProsjektIkon(ProsjektStatus? s) => ProsjektGruppe(s) switch
    {
        "tilbud" => "file-text",
        "ferdig" => "check",
        "service" => "tool",
        _ => "folder"
    };

    private static string ProsjektUndertekst(ProsjektRad p)
    {
        var deler = new[] { p.System, p.Dorer > 0 ? $"{p.Dorer} dører" : null }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        return deler.Count > 0 ? string.Join(" · ", deler) : p.Adresse ?? "";
    }

    // ---------------- Tilbud ----------------

    private List<int> TilbudAr => _tilbudListe.Select(t => t.OpprettetDato.ToLocalTime().Year)
        .Append(DateTime.Today.Year)
        .Distinct()
        .OrderByDescending(a => a)
        .ToList();

    private List<Tilbud> TilbudIAr => _tilbudListe
        .Where(t => _tilbudAr == 0 || t.OpprettetDato.ToLocalTime().Year == _tilbudAr)
        .ToList();

    private IEnumerable<Tilbud> FiltrerteTilbud => _tilbudFilter switch
    {
        "Utkast" => TilbudIAr.Where(t => t.Status == EndringsmeldingStatus.Utkast),
        "SendtTilKunde" => TilbudIAr.Where(t => t.Status == EndringsmeldingStatus.SendtTilKunde),
        "Godkjent" => TilbudIAr.Where(t => t.Status == EndringsmeldingStatus.Godkjent),
        "Avslatt" => TilbudIAr.Where(t => t.Status == EndringsmeldingStatus.Avslatt),
        _ => TilbudIAr
    };

    private string ArTekst => _tilbudAr == 0 ? "" : $" i {_tilbudAr}";

    private int? Hitrate
    {
        get
        {
            var vunnet = TilbudIAr.Count(t => t.Status == EndringsmeldingStatus.Godkjent);
            var avgjort = vunnet + TilbudIAr.Count(t => t.Status == EndringsmeldingStatus.Avslatt);
            return avgjort == 0 ? null : vunnet * 100 / avgjort;
        }
    }

    // Tid fra tilbudet ble sendt til kunden svarte (avslått-dato, ellers sist oppdatert).
    private int? SnittSvartid
    {
        get
        {
            var dager = TilbudIAr
                .Where(t => t.SendtDato.HasValue && t.Status is EndringsmeldingStatus.Godkjent or EndringsmeldingStatus.Avslatt)
                .Select(t => ((t.AvslattDato ?? t.OppdatertDato) - t.SendtDato)?.TotalDays)
                .Where(d => d is > 0)
                .Select(d => d!.Value)
                .ToList();
            return dager.Count == 0 ? null : (int)Math.Round(dager.Average());
        }
    }

    private static string TilbudStatusTekst(Tilbud t) => t.Status switch
    {
        EndringsmeldingStatus.Utkast => "Utkast",
        EndringsmeldingStatus.SendtTilKunde => $"Venter · {(DateTime.Today - (t.SendtDato ?? t.OpprettetDato.ToLocalTime()).Date).Days} d",
        EndringsmeldingStatus.Godkjent => "Vunnet",
        EndringsmeldingStatus.Avslatt => "Tapt",
        _ => t.Status.Visningsnavn()
    };

    private static string TilbudPillKlasse(EndringsmeldingStatus s) => s switch
    {
        EndringsmeldingStatus.SendtTilKunde => "s-gul",
        EndringsmeldingStatus.Godkjent => "s-ok",
        EndringsmeldingStatus.Avslatt => "s-rod",
        _ => "s-gra"
    };

    private void VelgTilbudAr(ChangeEventArgs e)
    {
        _tilbudAr = int.TryParse(e.Value?.ToString(), out var ar) ? ar : DateTime.Today.Year;
        _tilbudSide = 1;
    }

    private void VelgTilbudFilter(string filter)
    {
        _tilbudFilter = filter;
        _tilbudSide = 1;
    }

    private void FolgOppTilbud(Tilbud t)
    {
        NullstillOppfolgingSkjema();
        _ofTittel = $"Følg opp tilbud {t.Tittel}";
        _ofType = KundeOppfolgingType.Ring;
        if (!string.IsNullOrWhiteSpace(t.KontaktpersonNavn) && Kontaktnavn.Contains(t.KontaktpersonNavn))
        {
            _ofKontakt = t.KontaktpersonNavn;
        }
        VelgFane(KundeFane.Oppfolging);
    }

    // ---------------- Ordrer og tickets ----------------

    private IEnumerable<Arbeidsordre> FiltrerteOrdrer => _ordreFilter switch
    {
        "Planlagt" => _arbeidsordrer.Where(a => a.Status is ArbeidsordreStatus.Ny or ArbeidsordreStatus.Klar),
        "IArbeid" => _arbeidsordrer.Where(a => a.Status == ArbeidsordreStatus.Pagaende),
        "Ferdig" => _arbeidsordrer.Where(a => a.Status == ArbeidsordreStatus.Ferdig),
        "Fakturert" => _arbeidsordrer.Where(a => a.Status == ArbeidsordreStatus.Fakturert),
        _ => _arbeidsordrer
    };

    private static string OrdreStatusTekst(ArbeidsordreStatus s) => s switch
    {
        ArbeidsordreStatus.Ny => "Ny",
        ArbeidsordreStatus.Klar => "Planlagt",
        ArbeidsordreStatus.Pagaende => "Pågår",
        ArbeidsordreStatus.Ferdig => "Klar til faktura",
        ArbeidsordreStatus.Fakturert => "Fakturert",
        _ => s.Visningsnavn()
    };

    private static string OrdrePillKlasse(ArbeidsordreStatus s) => s switch
    {
        ArbeidsordreStatus.Klar => "s-lilla",
        ArbeidsordreStatus.Pagaende => "s-bla",
        ArbeidsordreStatus.Ferdig => "s-ok",
        _ => "s-gra"
    };

    private IEnumerable<Ticket> VisteTickets => _ticketFilter switch
    {
        "lukket" => _tickets.Where(t => t.Status == TicketStatus.Lukket),
        "alle" => _tickets,
        _ => _tickets.Where(t => t.Status != TicketStatus.Lukket)
    };

    private static string TicketSted(Ticket t)
    {
        var deler = new[] { t.Prosjekt?.Navn, t.Lokasjon }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        return deler.Count == 0 ? "–" : string.Join(" · ", deler);
    }

    private static string TicketPillKlasse(TicketStatus s) => s switch
    {
        TicketStatus.Ny => "s-rod",
        TicketStatus.UnderBehandling => "s-gul",
        TicketStatus.VenterGodkjenning => "s-lilla",
        _ => "s-gra"
    };

    private List<Produktgruppe> LedigeProduktgrupper => _produktgrupper
        .Where(p => _kundeRabatter.All(r => r.ProduktgruppeId != p.Id))
        .ToList();

    // ---------------- Hjelpere ----------------

    private static string Initialer(string navn)
    {
        var deler = navn.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return deler.Length switch
        {
            0 => "?",
            1 => deler[0][..Math.Min(2, deler[0].Length)].ToUpperInvariant(),
            _ => $"{deler[0][0]}{deler[^1][0]}".ToUpperInvariant()
        };
    }

    private static string AvatarFarge(string navn) => AvatarFarger[Math.Abs(navn.Sum(c => c)) % AvatarFarger.Length];

    private static string FormaterOrgNr(string orgNr)
    {
        var siffer = new string(orgNr.Where(char.IsDigit).ToArray());
        return siffer.Length == 9 ? $"{siffer[..3]} {siffer[3..6]} {siffer[6..]}" : orgNr;
    }

    private static string Kr(decimal belop) => belop >= 1_000_000
        ? $"{(belop / 1_000_000).ToString("0.#", Kultur)} mill"
        : $"{belop.ToString("N0", Kultur)} kr";

    private static string KortDato(DateTime d) => d.Year == DateTime.Today.Year ? d.ToString("dd.MM") : d.ToString("dd.MM.yy");

    private static string Stor(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Kultur) + s[1..];

    private static string RelativDato(DateTime d)
    {
        var dager = (d.Date - DateTime.Today).Days;
        return dager switch
        {
            0 => "i dag",
            1 => "i morgen",
            -1 => "i går – forfalt",
            var x when 0 > x => $"forfalt for {-x} dager siden",
            var x => $"om {x} dager"
        };
    }

    private static string FristTekst(KundeOppfolging o)
    {
        var dager = (o.Frist.Date - DateTime.Today).Days;
        if (0 > dager)
        {
            return $"Forfalt · {o.Frist:dd.MM} {o.Frist:HH:mm}";
        }
        return dager <= 6
            ? $"{Stor(dager == 0 ? "i dag" : dager == 1 ? "i morgen" : o.Frist.ToString("dddd", Kultur))} {o.Frist:HH:mm}"
            : $"{o.Frist:dd.MM.yyyy} {o.Frist:HH:mm}";
    }

    private static string VarselNarTekst(int dager) => dager switch
    {
        1 => "1 dag før",
        7 => "1 uke før",
        _ => $"{dager} dager før"
    };

    private void VelgFane(KundeFane f)
    {
        _fane = f;
        _visNyttMeny = false;
    }

    // ---------------- Lasting ----------------

    protected override async Task OnParametersSetAsync()
    {
        if (AuthStateTask is not null && _naverendeBrukerId is null)
        {
            var state = await AuthStateTask;
            var brukerClaim = state.User.FindFirst("BrukerId")?.Value;
            if (brukerClaim is not null && int.TryParse(brukerClaim, out var bid))
            {
                _naverendeBrukerId = bid;
                _naverendeBruker = await Db.Brukere.FindAsync(bid);
            }
        }

        if (_loadedId == Id)
        {
            return;
        }

        _loadedId = Id;
        _confirmingDelete = false;
        _error = null;
        _savedMessage = null;
        _nyttNotat = "";
        _nyKontaktpersonNavn = "";
        _tilbudAr = DateTime.Today.Year;
        _tilbudFilter = "Alle";
        _tilbudSide = 1;
        _fane = Fane?.ToLowerInvariant() switch
        {
            "oppfolging" => KundeFane.Oppfolging,
            "prosjekter" => KundeFane.Prosjekter,
            "tilbud" => KundeFane.Tilbud,
            "ordrer" => KundeFane.Ordrer,
            "tickets" => KundeFane.Tickets,
            "avtaler" or "lisenser" => KundeFane.Avtaler,
            "priser" or "rabatter" => KundeFane.Priser,
            "kontakter" or "kontaktpersoner" => KundeFane.Kontaktpersoner,
            "detaljer" => KundeFane.Detaljer,
            _ => KundeFane.Oversikt
        };

        _kunde = await Db.Kunder.FirstOrDefaultAsync(k => k.Id == Id);
        _notFound = _kunde is null;

        if (_kunde is null)
        {
            return;
        }

        var prosjekter = await Db.Prosjekter
            .Where(p => p.KundeId == Id)
            .Select(p => new ProsjektRad
            {
                Id = p.Id,
                Navn = p.Navn,
                Adresse = p.Adresse,
                System = p.System,
                Status = p.Status,
                Ansvarlig = p.Prosjektleder != null ? p.Prosjektleder.Navn : p.AnsvarligMontor != null ? p.AnsvarligMontor.Navn : null,
                Dorer = p.Dorer.Count,
                Ferdige = p.Dorer.Count(d => d.Status == MontasjeStatus.FerdigMontert)
            })
            .ToListAsync();
        string[] rekkefolge = ["pagar", "tilbud", "service", "annet", "ferdig"];
        _prosjekter = prosjekter.OrderBy(p => Array.IndexOf(rekkefolge, ProsjektGruppe(p.Status))).ThenBy(p => p.Navn).ToList();

        // AsNoTracking på listene under: verifisert at redigering/sletting av
        // enkeltelementer alltid gjøres via et FRISKT FirstOrDefaultAsync-
        // oppslag (se f.eks. LagreKontaktperson/SlettKontaktperson under),
        // ikke ved å mutere objektene i disse listene direkte. Se CODE_REVIEW.md 2026-10-08.
        _notater = await Db.KundeOppfolgingNotater
            .AsNoTracking()
            .Where(n => n.KundeId == Id)
            .Include(n => n.OpprettetAvBruker)
            .OrderByDescending(n => n.OpprettetDato)
            .ToListAsync();

        _kontaktpersoner = await Db.KundeKontaktpersoner
            .AsNoTracking()
            .Where(k => k.KundeId == Id)
            .OrderBy(k => k.Id)
            .ToListAsync();

        _arbeidsordrer = await Db.Arbeidsordre
            .AsNoTracking()
            .Where(a => a.KundeId == Id || a.Prosjekt!.KundeId == Id)
            .Include(a => a.Prosjekt)
            .Include(a => a.AnsvarligMontor)
            .OrderByDescending(a => a.OpprettetDato)
            .ToListAsync();

        _tilbudListe = await Db.Tilbud
            .AsNoTracking()
            .Where(t => t.Prosjekt!.KundeId == Id)
            .Include(t => t.Prosjekt)
            .OrderByDescending(t => t.OpprettetDato)
            .ToListAsync();

        _tickets = await Db.Tickets
            .AsNoTracking()
            .Where(t => t.KundeId == Id)
            .Include(t => t.Prosjekt)
            .OrderByDescending(t => t.OpprettetDato)
            .ToListAsync();

        _portalbrukere = await Db.Brukere.AsNoTracking().Where(b => b.KundeId == Id).OrderBy(b => b.Navn).ToListAsync();
        _ansatte = await Db.Brukere.AsNoTracking()
            .Where(b => b.Aktiv && b.Rolle != BrukerRolle.Kunde && b.Rolle != BrukerRolle.Driftslisens)
            .OrderBy(b => b.Navn)
            .ToListAsync();

        _produktgrupper = await Db.Produktgrupper.AsNoTracking().OrderBy(p => p.Navn).ToListAsync();
        _andreKunder = await Db.Kunder.AsNoTracking().Where(k => k.Id != Id).OrderBy(k => k.Navn).ToListAsync();
        await LastKundeRabatterAsync();
        await LastKundeLisenserAsync();
        await LastServiceavtalerAsync();
        await LastFakturertAsync();
        await LastOppfolgingerAsync();
        NullstillOppfolgingSkjema();
    }

    private async Task LastOppfolgingerAsync()
    {
        // Sporet (ikke AsNoTracking): Utført/Utsett/Endre endrer objektene direkte.
        _oppfolginger = await Db.KundeOppfolginger
            .Where(o => o.KundeId == Id)
            .Include(o => o.AnsvarligBruker)
            .Include(o => o.UtfortAvBruker)
            .OrderBy(o => o.Frist)
            .ToListAsync();
    }

    private async Task LastServiceavtalerAsync()
    {
        var avtaler = await Db.Prosjekter.AsNoTracking()
            .Where(p => p.KundeId == Id && p.Status == ProsjektStatus.Serviceavtale)
            .Select(p => new { p.Id, p.Navn, Dorer = p.Dorer.Count })
            .ToListAsync();
        var ider = avtaler.Select(a => a.Id).ToList();
        var runder = await Db.Servicerunder.AsNoTracking()
            .Where(r => ider.Contains(r.ProsjektId))
            .Select(r => new { r.ProsjektId, r.Dato, r.NesteServiceDato, r.ErPlanlagt })
            .ToListAsync();

        _serviceavtaler = avtaler.Select(a =>
        {
            var proRunder = runder.Where(r => r.ProsjektId == a.Id).OrderByDescending(r => r.Dato).ToList();
            return new ServiceavtaleRad(a.Id, a.Navn, a.Dorer,
                proRunder.FirstOrDefault(r => !r.ErPlanlagt)?.Dato,
                proRunder.FirstOrDefault(r => r.NesteServiceDato.HasValue)?.NesteServiceDato);
        }).ToList();
    }

    // «Fakturert i år» på Oversikt: samme beregning som Tripletex-eksporten av ordren.
    private async Task LastFakturertAsync()
    {
        var iAr = DateTime.Today.Year;
        var fra = new DateTime(iAr - 1, 1, 1);
        var ordrer = await Db.Arbeidsordre.AsNoTracking()
            .Where(a => (a.KundeId == Id || a.Prosjekt!.KundeId == Id) && a.FakturertDato != null && a.FakturertDato >= fra)
            .Include(a => a.Tilbud!).ThenInclude(t => t.Linjer).ThenInclude(l => l.Component!).ThenInclude(c => c.Inntektskonto)
            .Include(a => a.Varer).ThenInclude(v => v.Component!).ThenInclude(c => c.Inntektskonto)
            .AsSplitQuery()
            .ToListAsync();

        decimal Sum(int ar) => ordrer.Where(o => o.FakturertDato!.Value.Year == ar)
            .Sum(o => ArbeidsordreOkonomiBeregner.BeregnLinjer(o).Sum(l => l.Belop));
        _fakturertIAr = Sum(iAr);
        _fakturertIFjor = Sum(iAr - 1);
    }

    // ---------------- Oppfølging ----------------

    private void NullstillOppfolgingSkjema()
    {
        _ofRedigerId = null;
        _ofTittel = "";
        _ofType = KundeOppfolgingType.Ring;
        _ofDato = DateOnly.FromDateTime(DateTime.Today.AddDays(14));
        _ofTid = new TimeOnly(9, 0);
        _ofAnsvarligId = _ansatte.Any(b => b.Id == _naverendeBrukerId) ? _naverendeBrukerId : null;
        _ofKontakt = string.IsNullOrWhiteSpace(_kunde?.Kontaktperson) ? null : _kunde.Kontaktperson;
        _ofPortal = true;
        _ofEpost = true;
        _ofDagerFor = 0;
        _ofGjenta = KundeOppfolgingGjenta.Ikke;
        _ofNotat = null;
        _ofFeil = null;
    }

    private void NyOppfolgingFraMeny()
    {
        NullstillOppfolgingSkjema();
        VelgFane(KundeFane.Oppfolging);
    }

    private void RedigerOppfolging(KundeOppfolging o)
    {
        _ofRedigerId = o.Id;
        _ofTittel = o.Tittel;
        _ofType = o.Type;
        _ofDato = DateOnly.FromDateTime(o.Frist);
        _ofTid = TimeOnly.FromDateTime(o.Frist);
        _ofAnsvarligId = o.AnsvarligBrukerId;
        _ofKontakt = o.Kontaktperson;
        _ofPortal = o.VarselPortal;
        _ofEpost = o.VarselEpost;
        _ofDagerFor = o.VarselDagerFor;
        _ofGjenta = o.Gjenta;
        _ofNotat = o.Notat;
        _ofFeil = null;
        _ofMelding = null;
    }

    private async Task LagreOppfolging()
    {
        _ofFeil = null;
        _ofMelding = null;
        if (string.IsNullOrWhiteSpace(_ofTittel))
        {
            _ofFeil = "Skriv hva som skal gjøres.";
            return;
        }
        if (_ofDato is null)
        {
            _ofFeil = "Velg en dato.";
            return;
        }

        var frist = _ofDato.Value.ToDateTime(_ofTid);
        var o = _ofRedigerId is int rid ? _oppfolginger.FirstOrDefault(x => x.Id == rid) : null;
        if (o is null)
        {
            // En eldre «neste oppfølging» (bare dato på kunden) tas vare på som egen oppfølging.
            if (ApneOppfolginger.Count == 0 && _kunde!.NesteOppfolgingsDato is { } gammel)
            {
                Db.KundeOppfolginger.Add(new KundeOppfolging
                {
                    KundeId = Id,
                    Tittel = "Følg opp kunden",
                    Type = KundeOppfolgingType.Annet,
                    Frist = gammel.Date.AddHours(9),
                    VarselEpost = false
                });
            }

            o = new KundeOppfolging { KundeId = Id, Tittel = "" };
            Db.KundeOppfolginger.Add(o);
        }
        else if (o.Frist != frist || o.VarselDagerFor != _ofDagerFor)
        {
            o.EpostVarselSendt = false;
        }

        o.Tittel = _ofTittel.Trim();
        o.Type = _ofType;
        o.Frist = frist;
        o.AnsvarligBrukerId = _ofAnsvarligId;
        o.Kontaktperson = string.IsNullOrWhiteSpace(_ofKontakt) ? null : _ofKontakt;
        o.VarselPortal = _ofPortal;
        o.VarselEpost = _ofEpost;
        o.VarselSms = false;
        o.VarselDagerFor = _ofDagerFor;
        o.Gjenta = _ofGjenta;
        o.Notat = string.IsNullOrWhiteSpace(_ofNotat) ? null : _ofNotat.Trim();
        await Db.SaveChangesAsync();

        await LastOppfolgingerAsync();
        await SynkNesteOppfolgingAsync();
        _ofMelding = _ofPortal || _ofEpost
            ? $"Lagret – varsel {Stor(o.VarselDato.ToString("dddd d. MMMM", Kultur))}."
            : "Lagret (uten varsel).";
        NullstillOppfolgingSkjema();
    }

    private async Task UtsettOppfolging(KundeOppfolging o)
    {
        var start = o.Frist.Date < DateTime.Today ? DateTime.Today.Add(o.Frist.TimeOfDay) : o.Frist;
        o.Frist = start.AddDays(1);
        o.EpostVarselSendt = false;
        await Db.SaveChangesAsync();
        await SynkNesteOppfolgingAsync();
        _ofMelding = $"Utsatt til {o.Frist:dd.MM.yyyy}.";
    }

    private async Task FullforOppfolging(KundeOppfolging o)
    {
        o.Utfort = true;
        o.UtfortDato = DateTime.Now;
        o.UtfortAvBrukerId = _naverendeBrukerId;
        o.UtfortAvBruker = _naverendeBruker;

        if (o.Gjenta != KundeOppfolgingGjenta.Ikke)
        {
            Db.KundeOppfolginger.Add(new KundeOppfolging
            {
                KundeId = o.KundeId,
                Tittel = o.Tittel,
                Type = o.Type,
                Frist = o.Gjenta.NesteGang(o.Frist),
                AnsvarligBrukerId = o.AnsvarligBrukerId,
                Kontaktperson = o.Kontaktperson,
                Notat = o.Notat,
                VarselPortal = o.VarselPortal,
                VarselEpost = o.VarselEpost,
                VarselDagerFor = o.VarselDagerFor,
                Gjenta = o.Gjenta
            });
        }

        await Db.SaveChangesAsync();
        await LastOppfolgingerAsync();
        await SynkNesteOppfolgingAsync();
        _ofMelding = o.Gjenta == KundeOppfolgingGjenta.Ikke ? "Markert som utført." : $"Utført – neste er satt opp ({o.Gjenta.Visningsnavn().ToLower()}).";
    }

    private async Task FjernGammelOppfolging()
    {
        _kunde!.NesteOppfolgingsDato = null;
        await Db.SaveChangesAsync();
        _ofMelding = "Markert som utført.";
    }

    // Bjella og Hjem leser Kunde.NesteOppfolgingsDato: sett den til tidligste
    // varseldato blant åpne oppfølginger med portalvarsel.
    private async Task SynkNesteOppfolgingAsync()
    {
        var apne = _oppfolginger.Where(o => !o.Utfort && o.VarselPortal).ToList();
        _kunde!.NesteOppfolgingsDato = apne.Count == 0 ? null : apne.Min(o => o.VarselDato);
        await Db.SaveChangesAsync();
    }

    private async Task LeggTilNotat()
    {
        if (string.IsNullOrWhiteSpace(_nyttNotat))
        {
            return;
        }

        var notat = new KundeOppfolgingNotat
        {
            KundeId = Id,
            Tekst = _nyttNotat.Trim(),
            OpprettetAvBrukerId = _naverendeBrukerId,
            OpprettetAvBruker = _naverendeBruker
        };
        Db.KundeOppfolgingNotater.Add(notat);
        await Db.SaveChangesAsync();

        _notater.Insert(0, notat);
        _nyttNotat = "";
        _visLogg = false;
    }

    // ---------------- Lisenser ----------------

    private async Task LastKundeLisenserAsync()
    {
        // AsNoTracking: LagreLisens/SlettLisens henter alltid et friskt
        // oppslag før de endrer noe (se under), ikke via denne listen direkte.
        _kundeLisenser = await Db.KundeLisenser
            .AsNoTracking()
            .Where(l => l.KundeId == Id)
            .OrderBy(l => l.UtlopsDato)
            .ToListAsync();
    }

    private async Task LeggTilLisens()
    {
        _lisensFeil = null;
        _lisensMelding = null;

        if (string.IsNullOrWhiteSpace(_nyLisensNavn))
        {
            _lisensFeil = "Skriv inn navn på lisensen.";
            return;
        }

        Db.KundeLisenser.Add(new KundeLisens
        {
            KundeId = Id,
            Navn = _nyLisensNavn.Trim(),
            UtlopsDato = _nyLisensUtlopsDato
        });
        await Db.SaveChangesAsync();

        _nyLisensNavn = "";
        _nyLisensUtlopsDato = DateTime.Today.AddYears(1);
        _lisensMelding = "Lisens lagt til.";
        _visNyLisens = false;
        await LastKundeLisenserAsync();
    }

    private async Task LagreLisens(KundeLisens l)
    {
        var entitet = await Db.KundeLisenser.FirstOrDefaultAsync(x => x.Id == l.Id);
        if (entitet is null)
        {
            return;
        }

        entitet.Navn = l.Navn.Trim();
        entitet.UtlopsDato = l.UtlopsDato;
        entitet.Notat = l.Notat;
        await Db.SaveChangesAsync();
        _lisensFeil = null;
        _lisensMelding = "Lagret.";
    }

    private async Task SlettLisens(KundeLisens l)
    {
        var entitet = await Db.KundeLisenser.FirstOrDefaultAsync(x => x.Id == l.Id);
        if (entitet is null)
        {
            return;
        }

        Db.KundeLisenser.Remove(entitet);
        await Db.SaveChangesAsync();
        _lisensMelding = "Lisens fjernet.";
        await LastKundeLisenserAsync();
    }

    // ---------------- Rabatter og påslag ----------------

    private async Task LastKundeRabatterAsync()
    {
        // AsNoTracking: LagreRabatt/SlettRabatt henter alltid et friskt
        // oppslag før de endrer noe (se under), ikke via denne listen direkte.
        // KopierRabatterTilAnnenKunde LESER kun fra denne listen (oppretter
        // nye rader for en ANNEN kunde), muterer aldri elementene i den.
        _kundeRabatter = await Db.KundeRabatter
            .AsNoTracking()
            .Where(r => r.KundeId == Id)
            .Include(r => r.Produktgruppe)
            .OrderBy(r => r.Produktgruppe!.Navn)
            .ToListAsync();
    }

    private async Task LeggTilRabatt()
    {
        _rabattFeil = null;
        _rabattMelding = null;

        if (_nyRabattProduktgruppeId is null)
        {
            _rabattFeil = "Velg en produktgruppe.";
            return;
        }

        Db.KundeRabatter.Add(new KundeRabatt
        {
            KundeId = Id,
            ProduktgruppeId = _nyRabattProduktgruppeId.Value,
            RabattProsent = _nyRabattProsent
        });
        await Db.SaveChangesAsync();

        _nyRabattProduktgruppeId = null;
        _nyRabattProsent = 0;
        _rabattMelding = "Rabatt lagt til.";
        _visNyRabatt = false;
        await LastKundeRabatterAsync();
    }

    private async Task LagreRabatt(KundeRabatt r)
    {
        var entitet = await Db.KundeRabatter.FirstOrDefaultAsync(x => x.Id == r.Id);
        if (entitet is null)
        {
            return;
        }

        entitet.RabattProsent = r.RabattProsent;
        await Db.SaveChangesAsync();
        _rabattFeil = null;
        _rabattMelding = "Lagret.";
    }

    private async Task SlettRabatt(KundeRabatt r)
    {
        var entitet = await Db.KundeRabatter.FirstOrDefaultAsync(x => x.Id == r.Id);
        if (entitet is null)
        {
            return;
        }

        Db.KundeRabatter.Remove(entitet);
        await Db.SaveChangesAsync();
        _rabattMelding = "Rabatt fjernet.";
        await LastKundeRabatterAsync();
    }

    private async Task KopierRabatterTilAnnenKunde()
    {
        _rabattFeil = null;
        _rabattMelding = null;

        if (_kopierTilKundeId is null)
        {
            _rabattFeil = "Velg hvilken kunde rabattene skal kopieres til.";
            return;
        }

        var malKundeId = _kopierTilKundeId.Value;
        var eksisterende = await Db.KundeRabatter
            .Where(r => r.KundeId == malKundeId)
            .ToListAsync();

        foreach (var kilde in _kundeRabatter)
        {
            var treff = eksisterende.FirstOrDefault(r => r.ProduktgruppeId == kilde.ProduktgruppeId);
            if (treff is not null)
            {
                treff.RabattProsent = kilde.RabattProsent;
            }
            else
            {
                Db.KundeRabatter.Add(new KundeRabatt
                {
                    KundeId = malKundeId,
                    ProduktgruppeId = kilde.ProduktgruppeId,
                    RabattProsent = kilde.RabattProsent
                });
            }
        }

        await Db.SaveChangesAsync();
        var malNavn = _andreKunder.FirstOrDefault(k => k.Id == malKundeId)?.Navn ?? "kunden";
        _rabattMelding = $"Rabatter kopiert til {malNavn}.";
        _kopierTilKundeId = null;
        _visKopier = false;
    }

    private async Task LagreFastPaslag()
    {
        var entitet = await Db.Kunder.FirstOrDefaultAsync(k => k.Id == Id);
        if (entitet is null)
        {
            return;
        }

        entitet.FastPaslagProsent = _kunde!.FastPaslagProsent;
        await Db.SaveChangesAsync();
        _paslagMelding = "Fast påslag lagret.";
        _rabattFeil = null;
        await LastKundeRabatterAsync();
    }

    // ---------------- Ordre og ticket ----------------

    private async Task NyArbeidsordre()
    {
        var ordre = new Arbeidsordre { Tittel = "Ny arbeidsordre", KundeId = Id };
        Db.Arbeidsordre.Add(ordre);
        await Db.SaveChangesAsync();
        Nav.NavigateTo($"/arbeidsordre/{ordre.Id}");
    }

    private async Task OpprettTicket()
    {
        var tittel = _nyTicketTittel.Trim();
        if (tittel.Length == 0)
        {
            return;
        }

        var ticket = new Ticket { Tittel = tittel, KundeId = Id };
        Db.Tickets.Add(ticket);
        await Db.SaveChangesAsync();

        Nav.NavigateTo($"/ticket/{ticket.Id}");
    }

    // ---------------- Firma og Brønnøysund ----------------

    private async Task SokBronnoysund(ChangeEventArgs e)
    {
        _kunde!.Navn = e.Value?.ToString() ?? "";
        var q = _kunde.Navn.Trim();

        if (q.Length < 2)
        {
            _navnForslag = [];
            _visNavnForslag = false;
            _sokerNavn = false;
            return;
        }

        var generasjon = ++_sokGenerasjon;
        _sokerNavn = true;
        await Task.Delay(300);
        if (generasjon != _sokGenerasjon)
        {
            return;
        }

        var treff = await Bronnoysund.SokAsync(q);
        if (generasjon != _sokGenerasjon)
        {
            return;
        }

        _navnForslag = treff;
        _visNavnForslag = _navnForslag.Count > 0;
        _sokerNavn = false;
    }

    private async Task SkjulNavnForslagForsinket()
    {
        await Task.Delay(150);
        _visNavnForslag = false;
        StateHasChanged();
    }

    private void VelgFraBronnoysund(BronnoysundClient.Sokeresultat resultat)
    {
        _kunde!.Navn = resultat.Navn;
        _kunde.OrgNr = resultat.OrgNr;
        if (!string.IsNullOrWhiteSpace(resultat.Adresse)) { _kunde.Adresse = resultat.Adresse; }
        if (!string.IsNullOrWhiteSpace(resultat.Postnr)) { _kunde.Postnr = resultat.Postnr; }
        if (!string.IsNullOrWhiteSpace(resultat.Sted)) { _kunde.Sted = resultat.Sted; }

        _navnForslag = [];
        _visNavnForslag = false;
    }

    private async Task SlaOppOrgNr()
    {
        _orgNrFeil = null;

        if (string.IsNullOrWhiteSpace(_kunde?.OrgNr))
        {
            _orgNrFeil = "Skriv inn et organisasjonsnummer først.";
            return;
        }

        _slarOppOrgNr = true;
        var resultat = await Bronnoysund.SlaOppAsync(_kunde.OrgNr);
        _slarOppOrgNr = false;

        if (resultat is null)
        {
            _orgNrFeil = "Fant ingen enhet med dette organisasjonsnummeret i Brønnøysundregisteret.";
            return;
        }

        _kunde.Navn = resultat.Navn;
        if (!string.IsNullOrWhiteSpace(resultat.Adresse)) { _kunde.Adresse = resultat.Adresse; }
        if (!string.IsNullOrWhiteSpace(resultat.Postnr)) { _kunde.Postnr = resultat.Postnr; }
        if (!string.IsNullOrWhiteSpace(resultat.Sted)) { _kunde.Sted = resultat.Sted; }
    }

    // «Oppdater» i Brreg-stripen på Detaljer: hent og lagre med en gang.
    private async Task OppdaterFraBrreg()
    {
        _brregMelding = null;
        await SlaOppOrgNr();
        if (_orgNrFeil is null)
        {
            await Db.SaveChangesAsync();
            _brregMelding = $"navn og adresse er oppdatert fra registeret ({DateTime.Now:dd.MM.yyyy}).";
        }
    }

    private async Task AutoLagre() => await Db.SaveChangesAsync();

    private async Task LastOppBilde(InputFileChangeEventArgs e)
    {
        if (_kunde is null)
        {
            return;
        }

        _bildeFeil = null;
        var fil = e.File;
        if (fil.Size > 4_000_000)
        {
            _bildeFeil = $"\"{fil.Name}\" er for stor (maks 4 MB).";
            return;
        }

        using var buffer = new MemoryStream();
        await using (var stream = fil.OpenReadStream(maxAllowedSize: 4_000_000))
        {
            await stream.CopyToAsync(buffer);
        }

        _kunde.BildeData = buffer.ToArray();
        _kunde.BildeContentType = string.IsNullOrWhiteSpace(fil.ContentType) ? "application/octet-stream" : fil.ContentType;
        _kunde.BildeFilnavn = fil.Name;
        await Db.SaveChangesAsync();
        _bildeVersjon++;
    }

    private async Task Lagre()
    {
        if (string.IsNullOrWhiteSpace(_kunde!.Navn))
        {
            _error = "Kunden må ha et navn.";
            return;
        }

        _error = null;
        await Db.SaveChangesAsync();
        _savedMessage = "Lagret.";
        _redigerFirma = false;
        _redigerFaktura = false;
    }

    // ---------------- Kontaktpersoner ----------------

    private async Task LeggTilKontaktperson()
    {
        if (string.IsNullOrWhiteSpace(_nyKontaktpersonNavn))
        {
            return;
        }

        var kontaktperson = new KundeKontaktperson
        {
            KundeId = Id,
            Navn = _nyKontaktpersonNavn.Trim()
        };
        Db.KundeKontaktpersoner.Add(kontaktperson);
        await Db.SaveChangesAsync();

        _kontaktpersoner.Add(kontaktperson);
        _nyKontaktpersonNavn = "";
        _visNyKontakt = false;
        _redigerKontaktId = kontaktperson.Id;
    }

    private async Task LagreKontaktperson(KundeKontaktperson k)
    {
        var entitet = await Db.KundeKontaktpersoner.FirstOrDefaultAsync(x => x.Id == k.Id);
        if (entitet is null)
        {
            return;
        }

        entitet.Navn = k.Navn.Trim();
        entitet.Rolle = string.IsNullOrWhiteSpace(k.Rolle) ? null : k.Rolle.Trim();
        entitet.Telefon = string.IsNullOrWhiteSpace(k.Telefon) ? null : k.Telefon.Trim();
        entitet.Epost = string.IsNullOrWhiteSpace(k.Epost) ? null : k.Epost.Trim();
        await Db.SaveChangesAsync();
    }

    private async Task SlettKontaktperson(KundeKontaktperson k)
    {
        var entitet = await Db.KundeKontaktpersoner.FirstOrDefaultAsync(x => x.Id == k.Id);
        if (entitet is not null)
        {
            Db.KundeKontaktpersoner.Remove(entitet);
            await Db.SaveChangesAsync();
        }

        _kontaktpersoner.Remove(k);
        _redigerKontaktId = null;
    }

    private async Task SlettKunde()
    {
        Db.Kunder.Remove(_kunde!);
        await Db.SaveChangesAsync();
        Nav.NavigateTo("/kunder");
    }
}
