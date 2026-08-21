window.theme = (function () {
    const storageKey = 'itlock-theme';

    function isDark() {
        const attr = document.documentElement.getAttribute('data-theme');
        if (attr === 'dark') return true;
        if (attr === 'light') return false;
        return window.matchMedia('(prefers-color-scheme: dark)').matches;
    }

    function apply() {
        const stored = localStorage.getItem(storageKey);
        if (stored === 'dark' || stored === 'light') {
            document.documentElement.setAttribute('data-theme', stored);
        }
    }

    function toggle() {
        const nowDark = isDark();
        const next = nowDark ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem(storageKey, next);
        return next === 'dark';
    }

    function initButtons() {
        document.querySelectorAll('[data-theme-toggle]').forEach(function (btn) {
            btn.textContent = isDark() ? btn.dataset.labelLight : btn.dataset.labelDark;
        });
    }

    document.addEventListener('DOMContentLoaded', initButtons);

    return { isDark, apply, toggle, initButtons };
})();
