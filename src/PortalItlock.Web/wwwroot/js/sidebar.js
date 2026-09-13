// Gjenbrukbar sjekk for om vi er i mobilvisning (samme brytepunkt som CSS-
// mediaqueryene). Brukes fra C# via JS-interop der en ekte HTML disabled-
// attributt (ikke bare CSS-styling) trengs for at en låsing skal være
// pålitelig uansett hvilken stylesheet-versjon nettleseren har cachet.
window.erMobilvisning = function () {
    return window.matchMedia('(max-width: 640.98px)').matches;
};

// "Gå til fullversjon" på mobil: appens skrivebordslayout styres av CSS-
// mediaqueryer basert på den EKTE vindusbredden (window.matchMedia), som en
// ren JS/C#-boolean alene ikke kan overstyre. Løsningen (samme prinsipp som
// "Vis skrivebordsversjon" i ekte mobilnettlesere) er å sette et fast
// viewport-mål og selv regne ut riktig initial-scale slik at hele den brede
// layouten skaleres ned til å fylle skjermen - med user-scalable=no slik at
// man ikke kan klype-zoome/panorere unna det, samtidig som vanlig vertikal
// sideskrolling fortsatt virker helt normalt (det er en egen mekanisme,
// uavhengig av zoom/pan-låsen).
// Bredden holdes bevisst så nær skrivebord-brytepunktet (min-width: 641px i
// CSS-en) som mulig - stor nok til at skrivebordslayouten trigges, men ikke
// bredere enn nødvendig, siden alt blir tilsvarende MINDRE på skjermen jo
// bredere det virtuelle målet er (var altfor lite ved 1024).
window.fullversjon = (function () {
    const VIRTUELL_BREDDE = 700;

    function aktiver() {
        const meta = document.querySelector('meta[name="viewport"]');
        if (!meta) {
            return;
        }
        const skala = window.innerWidth / VIRTUELL_BREDDE;
        meta.setAttribute('content', 'width=' + VIRTUELL_BREDDE + ', initial-scale=' + skala + ', maximum-scale=' + skala + ', minimum-scale=' + skala + ', user-scalable=no');
    }

    function deaktiver() {
        const meta = document.querySelector('meta[name="viewport"]');
        if (!meta) {
            return;
        }
        meta.setAttribute('content', 'width=device-width, initial-scale=1.0, viewport-fit=cover');
    }

    return { aktiver, deaktiver };
})();

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
// kaldstart av appen. Den retter seg selv først når NOE tvinger frem en ny
// repaint, som f.eks. en sidenavigasjon - derfor virket det som man måtte
// "bytte fane først". Tvinger i stedet frem én reflow rett etter innlasting
// på mobil, slik at bunn-fanen havner riktig med det samme.
(function () {
    function tvingReflowAvBunnfane() {
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
    }

    window.addEventListener('load', function () {
        requestAnimationFrame(tvingReflowAvBunnfane);
        setTimeout(tvingReflowAvBunnfane, 300);
    });
})();
