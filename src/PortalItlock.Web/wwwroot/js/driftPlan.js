// Plantegning i driftsportalen: zoom (knapper, hjul, to fingre) og dra for å flytte.
// Prikkene ligger i samme lag som tegningen og skaleres tilbake (--z), så de har
// samme størrelse uansett zoom. Et trykk som egentlig var et drag åpner ikke døren.
export function koble(flate) {
    if (!flate || flate._dp) {
        return;
    }
    const lag = flate.querySelector('.dp-plan-lag');
    const bilde = lag && lag.querySelector('img');
    if (!lag || !bilde) {
        return;
    }
    const st = { z: 1, x: 0, y: 0, min: 0.3, maks: 6 };
    flate._dp = st;
    const visning = flate.parentElement.querySelector('[data-zoom-tall]');

    function tegn() {
        lag.style.transform = `translate(${st.x}px, ${st.y}px) scale(${st.z})`;
        lag.style.setProperty('--z', st.z);
        if (visning) {
            visning.textContent = Math.round(st.z * 100) + ' %';
        }
    }

    function zoomRundt(px, py, nyZ) {
        nyZ = Math.min(st.maks, Math.max(st.min, nyZ));
        st.x = px - (px - st.x) * (nyZ / st.z);
        st.y = py - (py - st.y) * (nyZ / st.z);
        st.z = nyZ;
        tegn();
    }

    // Hele tegningen synlig og midtstilt. Gir false hvis flaten eller bildet
    // ikke har fått størrelse ennå (f.eks. før CSS eller bildet er lastet).
    let tilpasset = false;
    function tilpass() {
        const b = flate.clientWidth, h = flate.clientHeight;
        const lb = lag.offsetWidth, lh = lag.offsetHeight;
        if (!b || !h || !lb || !lh) {
            return false;
        }
        st.z = Math.min(1, h / lh, b / lb);
        st.min = Math.min(0.3, st.z);
        st.x = (b - lb * st.z) / 2;
        st.y = (h - lh * st.z) / 2;
        tegn();
        tilpasset = true;
        return true;
    }

    // Første tilpasning skjer så snart både flaten og bildet har størrelse.
    const forsok = () => { if (!tilpasset) { tilpass(); } };
    bilde.addEventListener('load', forsok);
    if ('ResizeObserver' in window) {
        new ResizeObserver(forsok).observe(flate);
    }
    if (bilde.complete) {
        forsok();
    }

    flate.addEventListener('wheel', e => {
        e.preventDefault();
        const r = flate.getBoundingClientRect();
        zoomRundt(e.clientX - r.left, e.clientY - r.top, st.z * (e.deltaY < 0 ? 1.15 : 1 / 1.15));
    }, { passive: false });

    // Mus og penn: dra for å flytte. Touch håndteres for seg lenger ned.
    const pekere = new Map();
    let flyttet = false, start = null, pinch = null;

    flate.addEventListener('pointerdown', e => {
        if (e.pointerType === 'touch') {
            return;
        }
        pekere.set(e.pointerId, { x: e.clientX, y: e.clientY });
        if (pekere.size === 1) {
            flyttet = false;
            start = { px: e.clientX, py: e.clientY, x: st.x, y: st.y };
        } else if (pekere.size === 2) {
            const [a, b] = [...pekere.values()];
            pinch = { d: Math.hypot(a.x - b.x, a.y - b.y), z: st.z };
        }
    });

    window.addEventListener('pointermove', e => {
        if (!pekere.has(e.pointerId)) {
            return;
        }
        pekere.set(e.pointerId, { x: e.clientX, y: e.clientY });
        if (pekere.size === 2 && pinch) {
            const [a, b] = [...pekere.values()];
            const r = flate.getBoundingClientRect();
            flyttet = true;
            zoomRundt((a.x + b.x) / 2 - r.left, (a.y + b.y) / 2 - r.top, pinch.z * Math.hypot(a.x - b.x, a.y - b.y) / pinch.d);
        } else if (pekere.size === 1 && start) {
            const dx = e.clientX - start.px, dy = e.clientY - start.py;
            if (!flyttet && Math.hypot(dx, dy) < 5) {
                return;
            }
            flyttet = true;
            flate.classList.add('drar');
            st.x = start.x + dx;
            st.y = start.y + dy;
            tegn();
        }
    });

    const slipp = e => {
        pekere.delete(e.pointerId);
        if (pekere.size < 2) {
            pinch = null;
        }
        if (pekere.size === 0) {
            start = null;
            flate.classList.remove('drar');
        }
    };
    window.addEventListener('pointerup', slipp);
    window.addEventListener('pointercancel', slipp);

    // Touch: én finger blar siden som vanlig (touch-action: pan-y), to fingre
    // flytter og zoomer tegningen. Ellers kommer man seg ikke forbi plantegningen
    // på telefon, fordi den fyller nesten hele skjermen.
    let tp = null;
    const midt = (t, r) => ({ x: (t[0].clientX + t[1].clientX) / 2 - r.left, y: (t[0].clientY + t[1].clientY) / 2 - r.top });
    const avst = t => Math.hypot(t[0].clientX - t[1].clientX, t[0].clientY - t[1].clientY);

    flate.addEventListener('touchstart', e => {
        if (e.touches.length === 1) {
            flyttet = false;
            return;
        }
        if (e.touches.length === 2) {
            e.preventDefault();
            const r = flate.getBoundingClientRect();
            const m = midt(e.touches, r);
            tp = { d: avst(e.touches) || 1, z: st.z, px: (m.x - st.x) / st.z, py: (m.y - st.y) / st.z };
            flyttet = true;
        }
    }, { passive: false });

    flate.addEventListener('touchmove', e => {
        if (!tp || e.touches.length < 2) {
            return;
        }
        if (e.cancelable) {
            e.preventDefault();
        }
        const r = flate.getBoundingClientRect();
        const m = midt(e.touches, r);
        st.z = Math.min(st.maks, Math.max(st.min, tp.z * avst(e.touches) / tp.d));
        st.x = m.x - tp.px * st.z;
        st.y = m.y - tp.py * st.z;
        tegn();
    }, { passive: false });

    const touchSlutt = e => {
        if (e.touches.length < 2) {
            tp = null;
        }
    };
    flate.addEventListener('touchend', touchSlutt);
    flate.addEventListener('touchcancel', touchSlutt);

    // Klikk etter et drag skal ikke åpne døren under fingeren.
    flate.addEventListener('click', e => {
        if (flyttet) {
            e.stopPropagation();
            e.preventDefault();
            flyttet = false;
        }
    }, true);

    flate.parentElement.querySelectorAll('[data-zoom]').forEach(k => k.addEventListener('click', () => {
        const valg = k.dataset.zoom;
        if (valg === 'tilpass') {
            tilpass();
            return;
        }
        zoomRundt(flate.clientWidth / 2, flate.clientHeight / 2, st.z * (valg === 'inn' ? 1.3 : 1 / 1.3));
    }));
}
