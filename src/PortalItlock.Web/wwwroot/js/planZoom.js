// Zoom i plantegningen i montørappen. Knipe med to fingre zoomer bare
// tegningen, ikke hele siden, så dørarket åpner seg i vanlig størrelse.
// Punktet mellom fingrene blir liggende der det er mens man zoomer.
window.planZoom = (function () {
    var MIN = 100, MAKS = 400;

    function flate(rull) { return rull.querySelector('.mp-plan-flate'); }

    function bredde(rull) {
        var f = flate(rull);
        return f ? parseFloat(f.style.width) || MIN : MIN;
    }

    // Setter zoomen og holder punktet (px, py) i rulleflaten i ro.
    function sett(rull, ny, px, py) {
        var f = flate(rull);
        if (!f) return;
        var gammel = bredde(rull);
        ny = Math.max(MIN, Math.min(MAKS, ny));
        if (px === undefined) { px = rull.clientWidth / 2; py = rull.clientHeight / 2; }
        var x = rull.scrollLeft + px, y = rull.scrollTop + py, k = ny / gammel;
        f.style.width = ny + '%';
        rull.scrollLeft = x * k - px;
        rull.scrollTop = y * k - py;
    }

    function avstand(t) { return Math.hypot(t[0].clientX - t[1].clientX, t[0].clientY - t[1].clientY); }

    function koble(rull) {
        if (!rull || rull.dataset.planZoom) return;
        rull.dataset.planZoom = '1';
        var start = null;
        rull.addEventListener('touchstart', function (e) {
            if (e.touches.length === 2) {
                var r = rull.getBoundingClientRect();
                start = {
                    d: avstand(e.touches), z: bredde(rull),
                    px: (e.touches[0].clientX + e.touches[1].clientX) / 2 - r.left,
                    py: (e.touches[0].clientY + e.touches[1].clientY) / 2 - r.top
                };
            }
        }, { passive: true });
        rull.addEventListener('touchmove', function (e) {
            if (e.touches.length !== 2 || !start) return;
            e.preventDefault();
            sett(rull, start.z * avstand(e.touches) / start.d, start.px, start.py);
        }, { passive: false });
        rull.addEventListener('touchend', function (e) { if (e.touches.length < 2) start = null; });
        // Safari sender egne gesture-hendelser; de stoppes så siden ikke zoomer.
        rull.addEventListener('gesturestart', function (e) { e.preventDefault(); });
        rull.addEventListener('gesturechange', function (e) { e.preventDefault(); });
    }

    return {
        koble: function () { document.querySelectorAll('.mp-plan-rull').forEach(koble); },
        steg: function (knapp, d) {
            var rull = knapp.closest('.mp-plan').querySelector('.mp-plan-rull');
            sett(rull, bredde(rull) + d);
        },
        // Er siden likevel zoomet (knepet utenfor tegningen), settes den
        // tilbake til vanlig størrelse før dørarket vises.
        nullstillSide: function () {
            if (!window.visualViewport || window.visualViewport.scale <= 1.01) return;
            var m = document.querySelector('meta[name=viewport]');
            if (!m) return;
            var c = m.getAttribute('content');
            m.setAttribute('content', c + ', maximum-scale=1');
            setTimeout(function () { m.setAttribute('content', c); }, 400);
        }
    };
})();
