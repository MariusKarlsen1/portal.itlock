// Gjenbrukbar sjekk for om vi er i mobilvisning (samme brytepunkt som CSS-
// mediaqueryene). Brukes fra C# via JS-interop der en ekte HTML disabled-
// attributt (ikke bare CSS-styling) trengs for at en låsing skal være
// pålitelig uansett hvilken stylesheet-versjon nettleseren har cachet.
window.erMobilvisning = function () {
    return window.matchMedia('(max-width: 640.98px)').matches;
};

// MainLayout er statisk og kan derfor bare avgjøre "content-fullversjon"
// riktig for selve siden som først ble lastet (der ?fullversjon står i
// URL-en) - vanlig SPA-navigasjon videre derfra (f.eks. å trykke seg inn på
// Arbeidsordre fra fullversjon-forsiden) bytter bare ut @Body uten at
// MainLayout rendres på nytt, så klassen ble stående feil på alle andre
// sider. MobilFilterKnapp (som uansett already følger med Verktoylinje sin
// tilstand pålitelig på tvers av navigasjon) kaller denne etter hver
// rendering for å holde klassen synkron via ren DOM-manipulasjon i stedet.
window.settInnholdFullversjon = function (aktiv) {
    var el = document.getElementById('side-innhold');
    if (el) {
        el.classList.toggle('content-fullversjon', !!aktiv);
    }
};

window.sidebarMeny = (function () {
    const storageKey = 'itlock-sidebar';

    function isCollapsed() {
        return document.documentElement.getAttribute('data-sidebar') === 'collapsed';
    }

    function apply() {
        if (localStorage.getItem(storageKey) === 'collapsed') {
            document.documentElement.setAttribute('data-sidebar', 'collapsed');
        }
    }

    function toggle() {
        const next = isCollapsed() ? 'expanded' : 'collapsed';
        document.documentElement.setAttribute('data-sidebar', next);
        localStorage.setItem(storageKey, next);
        return next === 'collapsed';
    }

    // På mobil finnes ikke den smale/brede sidemeny-varianten, og hele
    // navigasjonsskuffen (Hjem, Tilbake, varsler, alle modul-lenker) skal ikke
    // være tilgjengelig der - mobilvisning er begrenset til bunn-fanen. Derfor
    // åpner ☰ i stedet den samme begrensede brukermenyen som MK-avataren.
    function toggleMeny() {
        if (window.matchMedia('(max-width: 640.98px)').matches) {
            document.querySelector('.bruker-avatar')?.click();
        } else {
            toggle();
        }
    }

    return { isCollapsed, apply, toggle, toggleMeny };
})();

// Speiler <title> i en synlig tekst i mobil-toppfeltet, siden Blazors <PageTitle>
// bare setter document.title og ikke er lett tilgjengelig fra et layout-komponent.
// Blazors "enhanced navigation" bytter ut hele <head>/<title>-elementet ved
// sidenavigasjon i stedet for å bare mutere teksten i det - en observer bundet
// direkte til det opprinnelige <title>-elementet slutter da å fange endringer,
// slik at tittelen ble stående igjen på forrige side til man fysisk refreshet.
// Observerer derfor <head> bredt (subtree), og lytter i tillegg på Blazors eget
// enhanced-navigation-event som en ekstra sikkerhet.
(function () {
    function oppdaterMobilTittel() {
        const el = document.getElementById('mobil-topptittel');
        if (el) {
            el.textContent = document.title || 'Full Kontroll';
        }
    }

    new MutationObserver(oppdaterMobilTittel).observe(document.head, {
        childList: true,
        subtree: true,
        characterData: true
    });
    document.addEventListener('DOMContentLoaded', oppdaterMobilTittel);
    if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
        window.Blazor.addEventListener('enhancedload', oppdaterMobilTittel);
    } else {
        document.addEventListener('enhancedload', oppdaterMobilTittel);
    }
    oppdaterMobilTittel();
})();

// Kjent iOS-kvirk i hjemskjerm-app-modus (standalone): den faste bunn-fanen
// (.mobil-tabbar, position:fixed;bottom:0) kan flyte for høyt med et tomt
// gap under seg ved kaldstart - Safari sin INTERNE sporing av "hvor er
// bunnen av viewporten" for position:fixed-elementer henger da etter det
// visuelle, ekte viewportet, og retter seg først når NOE (som en touch)
// tvinger Safari til å regne den om. Tidligere forsøk prøvde å FÅ Safari
// til å gjøre denne omregningen selv (reflow-triksing med display:none) -
// men en ren JS-stilendring på ETT element trigger ikke nødvendigvis
// Safaris interne viewport-omregning i det hele tatt, bare selve
// elementets egen boks. Regner derfor i stedet posisjonen helt selv, rett
// fra window.visualViewport (som ER pålitelig, i motsetning til CSS sin
// position:fixed-sporing), og setter den som en eksplisitt piksel-verdi -
// det gjetter ingenting og er ikke avhengig av at Safari "retter seg selv".
window.forankreBunnfane = function () {
    if (!window.matchMedia('(max-width: 640.98px)').matches) {
        return;
    }
    var el = document.querySelector('.mobil-tabbar');
    if (!el || !window.visualViewport) {
        return;
    }
    var vv = window.visualViewport;
    var avstandFraBunn = window.innerHeight - (vv.height + vv.offsetTop);
    el.style.bottom = Math.max(0, Math.round(avstandFraBunn)) + 'px';
};

(function () {
    function bindForsok() {
        requestAnimationFrame(window.forankreBunnfane);
        [50, 100, 200, 300, 500, 800, 1200, 1800, 2500, 3500].forEach(function (ms) {
            setTimeout(window.forankreBunnfane, ms);
        });
    }

    window.addEventListener('load', bindForsok);

    // KRITISK for "hjemskjerm-ikon lukket og åpnet igjen": iOS gjenoppretter
    // ofte PWA-en fra Safari sin bfcache (back-forward cache) i stedet for å
    // gjøre en helt fersk sideinnlasting ved gjenåpning - i så fall fyres
    // 'load' ALDRI. 'pageshow' med event.persisted===true er signalet for
    // akkurat denne gjenopprettingen.
    window.addEventListener('pageshow', function (event) {
        if (event.persisted) {
            bindForsok();
        }
    });

    // Samme idé for tilfellet der siden IKKE ble bfcache-gjenopprettet, men
    // fanen/appen likevel var skjult en stund (bakgrunn -> forgrunn).
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') {
            bindForsok();
        }
    });

    if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
        window.Blazor.addEventListener('enhancedload', window.forankreBunnfane);
    } else {
        document.addEventListener('enhancedload', window.forankreBunnfane);
    }

    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', window.forankreBunnfane);
        window.visualViewport.addEventListener('scroll', window.forankreBunnfane);
    }

    window.addEventListener('resize', window.forankreBunnfane);

    // Behold navnet fra forrige forsøk som alias, i tilfelle noe fortsatt
    // kaller det direkte (f.eks. en bufret, ikke helt oppdatert kopi av
    // MobilTabBar sin egen render-hook).
    window.tvingReflowAvBunnfane = window.forankreBunnfane;
})();
