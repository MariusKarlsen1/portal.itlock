window.theme = (function () {
    const storageKey = 'itlock-theme';
    const media = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;

    // Valgt modus: 'light', 'dark' eller 'auto' (følger systemet).
    function mode() {
        const stored = localStorage.getItem(storageKey);
        return stored === 'dark' || stored === 'light' ? stored : 'auto';
    }

    function systemDark() {
        return !!(media && media.matches);
    }

    function isDark() {
        const attr = document.documentElement.getAttribute('data-theme');
        if (attr === 'dark') return true;
        if (attr === 'light') return false;
        return systemDark();
    }

    // data-theme settes alltid (også i Auto), så de mørke reglene bare
    // trenger å se på attributtet.
    function apply() {
        const m = mode();
        const resolved = m === 'auto' ? (systemDark() ? 'dark' : 'light') : m;
        document.documentElement.setAttribute('data-theme', resolved);
        initButtons();
    }

    function set(m) {
        if (m === 'dark' || m === 'light') {
            localStorage.setItem(storageKey, m);
        } else {
            localStorage.removeItem(storageKey);
        }
        apply();
        return isDark();
    }

    function toggle() {
        return set(isDark() ? 'light' : 'dark');
    }

    function initButtons() {
        document.querySelectorAll('[data-theme-toggle]').forEach(function (btn) {
            btn.textContent = isDark() ? btn.dataset.labelLight : btn.dataset.labelDark;
        });
    }

    // I Auto følger portalen med når systemet bytter mellom lys og mørk.
    if (media) {
        const onChange = function () {
            if (mode() === 'auto') {
                apply();
            }
        };
        if (media.addEventListener) {
            media.addEventListener('change', onChange);
        } else if (media.addListener) {
            media.addListener(onChange);
        }
    }

    // Skrifttype: «standard» (Fraunces i overskrifter) eller «vanlig» (en
    // vanlig skrift overalt). Lagres i nettleseren, som temaet.
    const fontKey = 'itlock-font';

    function font() {
        return localStorage.getItem(fontKey) === 'vanlig' ? 'vanlig' : 'standard';
    }

    function setFont(f) {
        if (f === 'vanlig') {
            localStorage.setItem(fontKey, 'vanlig');
            document.documentElement.setAttribute('data-font', 'vanlig');
        } else {
            localStorage.removeItem(fontKey);
            document.documentElement.removeAttribute('data-font');
        }
        return font();
    }

    // Blazor sin raske sidenavigasjon bytter ut attributtene på <html> med det
    // serveren sender, så valgene i nettleseren (tema, skrift, biler, meny)
    // forsvant ved første klikk i menyen. Settes tilbake fra localStorage
    // etter hver navigasjon, og om noe annet fjerner dem.
    function gjenopprett() {
        const root = document.documentElement;
        const m = mode();
        const tema = m === 'auto' ? (systemDark() ? 'dark' : 'light') : m;
        if (root.getAttribute('data-theme') !== tema) root.setAttribute('data-theme', tema);
        const valg = [
            ['data-font', localStorage.getItem(fontKey) === 'vanlig' ? 'vanlig' : null],
            ['data-biler', localStorage.getItem('itlock-biler') === 'kjorer' ? 'kjorer' : null],
            ['data-sidebar', localStorage.getItem('itlock-sidebar') === 'collapsed' ? 'collapsed' : null],
        ];
        valg.forEach(function (v) {
            if (v[1] && root.getAttribute(v[0]) !== v[1]) root.setAttribute(v[0], v[1]);
        });
    }

    new MutationObserver(function () {
        if (!document.documentElement.hasAttribute('data-theme')) {
            gjenopprett();
        }
    }).observe(document.documentElement, { attributes: true });

    function lyttEtterNavigasjon() {
        if (window.Blazor && window.Blazor.addEventListener) {
            window.Blazor.addEventListener('enhancedload', gjenopprett);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        initButtons();
        lyttEtterNavigasjon();
    });

    return { mode, isDark, apply, set, toggle, initButtons, font, setFont, gjenopprett };
})();
