// Gjenbrukbar sjekk for om vi er i mobilvisning (samme brytepunkt som CSS-
// mediaqueryene). Brukes fra C# via JS-interop der en ekte HTML disabled-
// attributt (ikke bare CSS-styling) trengs for at en låsing skal være
// pålitelig uansett hvilken stylesheet-versjon nettleseren har cachet.
window.erMobilvisning = function () {
    return window.matchMedia('(max-width: 640.98px)').matches;
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
(function () {
    function oppdaterMobilTittel() {
        const el = document.getElementById('mobil-topptittel');
        if (el) {
            el.textContent = document.title || 'Full Kontroll';
        }
    }

    const titleEl = document.querySelector('title');
    if (titleEl) {
        new MutationObserver(oppdaterMobilTittel).observe(titleEl, { childList: true });
    }
    document.addEventListener('DOMContentLoaded', oppdaterMobilTittel);
    oppdaterMobilTittel();
})();
