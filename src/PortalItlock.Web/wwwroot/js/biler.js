window.biler = (function () {
    const storageKey = 'itlock-biler';

    function erAktiv() {
        return document.documentElement.getAttribute('data-biler') === 'kjorer';
    }

    function frysPosisjon() {
        document.querySelectorAll('.home-drive-track').forEach(function (el) {
            el.style.left = getComputedStyle(el).left;
        });
        document.querySelectorAll('.home-drive-tilt').forEach(function (el) {
            el.style.transform = getComputedStyle(el).transform;
        });
        document.querySelectorAll('.home-drive-shadow').forEach(function (el) {
            const style = getComputedStyle(el);
            el.style.opacity = style.opacity;
            el.style.transform = style.transform;
        });
    }

    function fjernFrys() {
        document.querySelectorAll('.home-drive-track, .home-drive-tilt, .home-drive-shadow').forEach(function (el) {
            el.style.left = '';
            el.style.transform = '';
            el.style.opacity = '';
        });
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
            frysPosisjon();
            document.documentElement.removeAttribute('data-biler');
            localStorage.setItem(storageKey, 'stoppet');
            return false;
        }

        fjernFrys();
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
