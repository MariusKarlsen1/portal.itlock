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
// (.mobil-tabbar, position:fixed;bottom:0) kan bli stående "fastlåst" på en
// midlertidig feil posisjon fra aller første maling - før Safari sin egen
// visual viewport (adressefelt-animasjon m.m.) har rukket å sette seg ved
// kaldstart av appen, eller ved en forceLoad-navigasjon (f.eks. "Gå til
// fullversjon" og tilbake, som i praksis ER en ny sideinnlasting). Den
// retter seg selv først når NOE tvinger frem en ny repaint - derfor virket
// det som man måtte "bytte fane først". Én enkelt forsinkelse etter 'load'
// var upålitelig (Safari sin egen animasjon varierer i lengde med enhet/
// nettverk), så nå kombineres flere uavhengige triggere: gjentatte forsøk
// med økende forsinkelse, selve visualViewport-resize-eventet (fanger opp
// NÅR Safari faktisk er ferdig, i stedet for å gjette et tidspunkt), og et
// kall rett fra MobilTabBar.razor sin egen OnAfterRenderAsync - som er det
// mest pålitelige tidspunktet av alle, siden det garantert kjører først
// etter at Blazor faktisk har malt bunn-fanen i DOM-en (dekker "bytte
// tilbake fra fullversjon", som alltid er en fersk krets/render).
window.tvingReflowAvBunnfane = function () {
    if (!window.matchMedia('(max-width: 640.98px)').matches) {
        return;
    }
    var el = document.querySelector('.mobil-tabbar');
    if (!el) {
        return;
    }
    el.style.display = 'none';
    void el.offsetHeight;
    el.style.display = '';
};

(function () {
    function bindReflowForsok() {
        requestAnimationFrame(window.tvingReflowAvBunnfane);
        [100, 300, 600, 1000, 1800, 2500].forEach(function (ms) {
            setTimeout(window.tvingReflowAvBunnfane, ms);
        });
    }

    window.addEventListener('load', bindReflowForsok);

    // KRITISK for "hjemskjerm-ikon lukket og åpnet igjen": iOS gjenoppretter
    // ofte PWA-en fra Safari sin bfcache (back-forward cache) i stedet for å
    // gjøre en helt fersk sideinnlasting ved gjenåpning - i så fall fyres
    // 'load' ALDRI, og ingenting over kjørte i det hele tatt. 'pageshow' med
    // event.persisted===true er nettopp signalet for akkurat denne
    // gjenopprettingen, og er den som faktisk manglet.
    window.addEventListener('pageshow', function (event) {
        if (event.persisted) {
            bindReflowForsok();
        }
    });

    // Samme idé for tilfellet der siden IKKE ble bfcache-gjenopprettet, men
    // fanen/appen likevel var skjult en stund (bakgrunn -> forgrunn) - Safari
    // sin egen viewport-animasjon kan da også trenge å bli tvunget til å
    // sette seg på nytt.
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') {
            bindReflowForsok();
        }
    });

    if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
        window.Blazor.addEventListener('enhancedload', window.tvingReflowAvBunnfane);
    } else {
        document.addEventListener('enhancedload', window.tvingReflowAvBunnfane);
    }

    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', window.tvingReflowAvBunnfane);
    }
})();
