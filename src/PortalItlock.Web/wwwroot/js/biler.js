window.biler = (function () {
    const storageKey = 'itlock-biler';

    function erAktiv() {
        return document.documentElement.getAttribute('data-biler') === 'kjorer';
    }

    function apply() {
        const stored = localStorage.getItem(storageKey);
        if (stored === 'kjorer') {
            document.documentElement.setAttribute('data-biler', 'kjorer');
        }
    }

    function toggle() {
        const naAktiv = erAktiv();
        if (naAktiv) {
            document.documentElement.removeAttribute('data-biler');
            localStorage.setItem(storageKey, 'stoppet');
            return false;
        }

        document.documentElement.setAttribute('data-biler', 'kjorer');
        localStorage.setItem(storageKey, 'kjorer');
        return true;
    }

    function initButtons() {
        document.querySelectorAll('[data-biler-toggle]').forEach(function (btn) {
            btn.textContent = erAktiv() ? btn.dataset.labelStopp : btn.dataset.labelStart;
        });
    }

    document.addEventListener('DOMContentLoaded', initButtons);

    return { erAktiv, apply, toggle, initButtons };
})();
