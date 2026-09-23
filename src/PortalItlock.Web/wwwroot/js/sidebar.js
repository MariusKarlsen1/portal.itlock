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

// Den generiske "ett steg tilbake"-lenken øverst på hver side (se
// MainLayout.razor sin GeneriskTilbakeMal) regner alltid ut samme statiske
// forelder-hub for en gitt side, uavhengig av hvordan brukeren faktisk kom
// dit. Det stemmer når man kommer via selve hub-siden (f.eks.
// /befaringsmodul -> /befaringsliste), men er feil når man i stedet kom via
// forsidens modul-modal, som hopper rett fra "/" til undersiden uten
// noensinne å vise hub-siden - da skal tilbake-lenken gå rett til forsiden.
// Lenkene inni modul-modalen (se Home.razor) setter derfor et sessionStorage-
// flagg rett før navigering; her leses det av og brukes til å overstyre
// lenken denne ene gangen, før flagget fjernes igjen - et senere, vanlig
// besøk på samme side (f.eks. via hub-siden) skal fortsatt vise riktig
// statisk forelder.
(function () {
    var FLAGG_NOKKEL = 'itlock-kom-fra-hjem';

    function overstyrTilbakeTilHjemOmNodvendig() {
        var komFraHjem = false;
        try {
            komFraHjem = sessionStorage.getItem(FLAGG_NOKKEL) === '1';
            sessionStorage.removeItem(FLAGG_NOKKEL);
        } catch (e) {
            return;
        }
        if (!komFraHjem) {
            return;
        }

        var lenke = document.querySelector('.content-kompakt-tilbake');
        if (!lenke) {
            return;
        }
        lenke.setAttribute('href', '/');
        while (lenke.lastChild && lenke.lastChild.nodeType === Node.TEXT_NODE) {
            lenke.removeChild(lenke.lastChild);
        }
        lenke.appendChild(document.createTextNode('Til hjem'));
    }

    document.addEventListener('DOMContentLoaded', overstyrTilbakeTilHjemOmNodvendig);
    if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
        window.Blazor.addEventListener('enhancedload', overstyrTilbakeTilHjemOmNodvendig);
    } else {
        document.addEventListener('enhancedload', overstyrTilbakeTilHjemOmNodvendig);
    }
    overstyrTilbakeTilHjemOmNodvendig();
})();

// Kjent iOS-kvirk i hjemskjerm-app-modus (standalone): den faste bunn-fanen
// (.mobil-tabbar, position:fixed;bottom:0) kan flyte for høyt med et tomt
// gap under seg ved kaldstart. Flere runder med CSS-triksing (translateZ,
// backface-visibility) og JS-triksing (tvunget reflow, egenutregnet
// posisjon fra visualViewport) løste det IKKE - og et par av forsøkene satte
// i tillegg en egen inline "bottom"-verdi som kunne stå og forstyrre
// permanent, uten skikkelig måte å verifisere resultatet på et ekte device.
// Går derfor bevisst tilbake til ren CSS (ingen JS-satt inline "bottom" i
// det hele tatt), og legger i stedet inn nøyaktig det som opprinnelig ble
// foreslått: sjekk om fanen faktisk ligger feil (dens nedre kant skal være
// helt nede ved skjermens bunn), og reload siden ÉN gang for et
// garantert korrekt resultat i stedet for å gjette videre. sessionStorage
// hindrer en reload-løkke.
(function () {
    var RELOAD_NOKKEL = 'itlock-bunnfane-reload-forsokt';

    function sjekkOgReloadVedFeilPosisjon() {
        if (!window.matchMedia('(max-width: 640.98px)').matches) {
            return;
        }
        var el = document.querySelector('.mobil-tabbar');
        if (!el) {
            return;
        }

        var rect = el.getBoundingClientRect();
        var avvikFraBunn = Math.abs(rect.bottom - window.innerHeight);
        if (avvikFraBunn <= 8) {
            // Riktig plassert - nullstill vokteren slik at en EKTE feil
            // senere (f.eks. neste kaldstart) fortsatt fanges opp.
            try { sessionStorage.removeItem(RELOAD_NOKKEL); } catch (e) { }
            return;
        }

        var alleredeForsokt = false;
        try { alleredeForsokt = sessionStorage.getItem(RELOAD_NOKKEL) === '1'; } catch (e) { }
        if (alleredeForsokt) {
            return;
        }

        try { sessionStorage.setItem(RELOAD_NOKKEL, '1'); } catch (e) { }
        window.location.reload();
    }

    function planleggSjekk() {
        // Vent til Safari sin egen viewport-animasjon rimelig sikkert har
        // fått tid til å sette seg naturlig, før vi i det hele tatt sjekker.
        setTimeout(sjekkOgReloadVedFeilPosisjon, 1200);
        setTimeout(sjekkOgReloadVedFeilPosisjon, 2500);
    }

    window.addEventListener('load', planleggSjekk);

    // KRITISK for "hjemskjerm-ikon lukket og åpnet igjen": iOS gjenoppretter
    // ofte PWA-en fra Safari sin bfcache (back-forward cache) i stedet for å
    // gjøre en helt fersk sideinnlasting ved gjenåpning - i så fall fyres
    // 'load' ALDRI. 'pageshow' med event.persisted===true er signalet for
    // akkurat denne gjenopprettingen.
    window.addEventListener('pageshow', function (event) {
        if (event.persisted) {
            planleggSjekk();
        }
    });

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') {
            planleggSjekk();
        }
    });
})();
