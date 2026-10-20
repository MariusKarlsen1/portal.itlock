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

    document.addEventListener('DOMContentLoaded', initButtons);

    return { mode, isDark, apply, set, toggle, initButtons };
})();
