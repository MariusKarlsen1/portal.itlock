// Zoom med musehjulet på tegninger i oversikter (prosjektkortet og dørkortet).
// Holder man musen over en boks med [data-hjulzoom], zoomer hjulet rundt
// musepekeren; dra for å flytte når det er zoomet, dobbeltklikk for å
// tilbakestille. Selve flaten ([data-hjulzoom-flate]) skaleres, og pinnene
// ligger inni den, så de følger tegningen. --hz settes på flaten slik at
// pinnene kan skaleres tilbake og beholde størrelsen.
(function () {
    var MAKS = 6, STEG = 1.15, TERSKEL = 4;

    function tilstand(el) { return el._hz || (el._hz = { s: 1, x: 0, y: 0 }); }
    function flate(el) { return el.querySelector('[data-hjulzoom-flate]'); }

    function bruk(el) {
        var f = flate(el), z = tilstand(el);
        if (!f) return;
        var w = f.offsetWidth, h = f.offsetHeight;
        z.x = Math.min(0, Math.max(w - w * z.s, z.x));
        z.y = Math.min(0, Math.max(h - h * z.s, z.y));
        f.style.transformOrigin = '0 0';
        f.style.transform = z.s > 1.001 ? 'translate(' + z.x + 'px,' + z.y + 'px) scale(' + z.s + ')' : '';
        f.style.setProperty('--hz', z.s);
        el.classList.toggle('hz-zoomet', z.s > 1.001);
    }

    // Punkt i flatens egne (uskalerte) koordinater.
    function punkt(el, e) {
        var f = flate(el), r = el.getBoundingClientRect();
        return { x: e.clientX - r.left - f.offsetLeft + el.scrollLeft, y: e.clientY - r.top - f.offsetTop + el.scrollTop };
    }

    document.addEventListener('wheel', function (e) {
        var el = e.target.closest && e.target.closest('[data-hjulzoom]');
        if (!el || !flate(el)) return;
        e.preventDefault();
        var z = tilstand(el), p = punkt(el, e);
        var ny = Math.min(MAKS, Math.max(1, z.s * (e.deltaY < 0 ? STEG : 1 / STEG)));
        var k = ny / z.s;
        z.x = p.x - (p.x - z.x) * k;
        z.y = p.y - (p.y - z.y) * k;
        z.s = ny;
        bruk(el);
    }, { passive: false });

    var dra = null;
    document.addEventListener('pointerdown', function (e) {
        var el = e.target.closest && e.target.closest('[data-hjulzoom]');
        if (!el || e.button !== 0 || tilstand(el).s <= 1.001) return;
        // Ellers starter nettleseren sin egen «dra bildet» eller tekstmarkering,
        // og flyttingen stopper halvveis.
        e.preventDefault();
        dra = { el: el, id: e.pointerId, sx: e.clientX, sy: e.clientY, x0: tilstand(el).x, y0: tilstand(el).y, flyttet: false };
    });
    document.addEventListener('dragstart', function (e) {
        if (e.target.closest && e.target.closest('[data-hjulzoom]')) e.preventDefault();
    });
    document.addEventListener('pointermove', function (e) {
        if (!dra || e.pointerId !== dra.id) return;
        // Slapp man knappen utenfor vinduet, avsluttes flyttingen.
        if (!(e.buttons & 1)) { slutt(); return; }
        e.preventDefault();
        var dx = e.clientX - dra.sx, dy = e.clientY - dra.sy;
        if (!dra.flyttet && Math.hypot(dx, dy) < TERSKEL) return;
        dra.flyttet = true;
        dra.el.classList.add('hz-drar');
        var z = tilstand(dra.el);
        z.x = dra.x0 + dx;
        z.y = dra.y0 + dy;
        bruk(dra.el);
    });
    function slutt() {
        if (!dra) return;
        dra.el.classList.remove('hz-drar');
        // Et klikk rett etter at man har dratt skal ikke åpne en dør.
        if (dra.flyttet) dra.el._hzSlukKlikk = true;
        dra = null;
    }
    document.addEventListener('pointerup', slutt);
    document.addEventListener('pointercancel', slutt);
    window.addEventListener('blur', slutt);
    document.addEventListener('click', function (e) {
        var el = e.target.closest && e.target.closest('[data-hjulzoom]');
        if (el && el._hzSlukKlikk) {
            el._hzSlukKlikk = false;
            e.preventDefault();
            e.stopPropagation();
        }
    }, true);
    document.addEventListener('dblclick', function (e) {
        var el = e.target.closest && e.target.closest('[data-hjulzoom]');
        if (!el) return;
        var z = tilstand(el);
        z.s = 1; z.x = 0; z.y = 0;
        bruk(el);
    });
})();
