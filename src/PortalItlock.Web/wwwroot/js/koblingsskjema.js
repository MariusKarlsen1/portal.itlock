function safeCapture(el, pointerId) {
    try {
        el.setPointerCapture(pointerId);
    } catch {
        // Kan skje for syntetiske/inaktive pointere - ignorer.
    }
}

function safeRelease(el, pointerId) {
    try {
        el.releasePointerCapture(pointerId);
    } catch {
        // Kan skje for syntetiske/inaktive pointere - ignorer.
    }
}

export function getClickPercent(containerEl, clientX, clientY) {
    const rect = containerEl.getBoundingClientRect();
    const x = Math.max(0, Math.min(100, ((clientX - rect.left) / rect.width) * 100));
    const y = Math.max(0, Math.min(100, ((clientY - rect.top) / rect.height) * 100));
    return { x, y };
}

export function initZoomPan(wrapEl, canvasEl) {
    if (wrapEl.dataset.zoomPanBound === '1') {
        return;
    }
    wrapEl.dataset.zoomPanBound = '1';

    const minZoom = 0.5;
    const maxZoom = 3;
    const step = 0.12;
    let zoom = 1;

    canvasEl.style.width = '100%';

    function applyZoom(nextZoom, cursorX, cursorY) {
        const prevZoom = zoom;
        zoom = Math.min(maxZoom, Math.max(minZoom, nextZoom));
        if (zoom === prevZoom) {
            return;
        }

        const ratio = zoom / prevZoom;
        canvasEl.style.width = (zoom * 100) + '%';
        wrapEl.scrollLeft = (wrapEl.scrollLeft + cursorX) * ratio - cursorX;
        wrapEl.scrollTop = (wrapEl.scrollTop + cursorY) * ratio - cursorY;
    }

    wrapEl.addEventListener('wheel', (e) => {
        e.preventDefault();
        const rect = wrapEl.getBoundingClientRect();
        applyZoom(zoom + (e.deltaY < 0 ? step : -step), e.clientX - rect.left, e.clientY - rect.top);
    }, { passive: false });

    const dragThreshold = 4;
    let panning = false;
    let moved = false;
    let startX = 0;
    let startY = 0;
    let startScrollLeft = 0;
    let startScrollTop = 0;
    let suppressClick = false;

    // Aktive pekere holdes rede på for å skille mellom vanlig panorering med
    // én finger/mus og knip-for-å-zoome med to fingre på touch-skjermer -
    // .kobling-canvas har touch-action:none (nødvendig for å kunne dra
    // symboler uten at siden scroller samtidig), så nettleserens innebygde
    // knip-zoom er skrudd av og må derfor gjenskapes her selv.
    const activePointers = new Map();
    let pinching = false;
    let pinchStartDist = 0;
    let pinchStartZoom = 1;

    function pinchDistance() {
        const pts = [...activePointers.values()];
        return Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y);
    }

    function pinchMidpoint() {
        const pts = [...activePointers.values()];
        const rect = wrapEl.getBoundingClientRect();
        return {
            x: (pts[0].x + pts[1].x) / 2 - rect.left,
            y: (pts[0].y + pts[1].y) / 2 - rect.top
        };
    }

    // Hold inne høyre museknapp og dra for å flytte rundt på skissen, som på
    // plantegningen. Virker også mens man redigerer og tegner kabler.
    let hoyrePan = null;
    wrapEl.addEventListener('contextmenu', (e) => e.preventDefault());
    wrapEl.addEventListener('pointerdown', (e) => {
        if (e.button !== 2 || e.pointerType === 'touch') {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        hoyrePan = { x: e.clientX, y: e.clientY, l: wrapEl.scrollLeft, t: wrapEl.scrollTop };
        safeCapture(wrapEl, e.pointerId);
        wrapEl.classList.add('ks-panorerer');
    }, true);
    wrapEl.addEventListener('pointermove', (e) => {
        if (!hoyrePan) {
            return;
        }
        e.stopPropagation();
        wrapEl.scrollLeft = hoyrePan.l - (e.clientX - hoyrePan.x);
        wrapEl.scrollTop = hoyrePan.t - (e.clientY - hoyrePan.y);
    }, true);
    const slippHoyre = (e) => {
        if (!hoyrePan) {
            return;
        }
        hoyrePan = null;
        safeRelease(wrapEl, e.pointerId);
        wrapEl.classList.remove('ks-panorerer');
    };
    wrapEl.addEventListener('pointerup', slippHoyre, true);
    wrapEl.addEventListener('pointercancel', slippHoyre, true);

    wrapEl.addEventListener('click', (e) => {
        if (suppressClick) {
            suppressClick = false;
            e.stopPropagation();
            e.preventDefault();
        }
    }, true);

    wrapEl.addEventListener('pointerdown', (e) => {
        if (canvasEl.dataset.locked === '0') {
            return;
        }

        if (e.pointerType === 'touch') {
            activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
            safeCapture(wrapEl, e.pointerId);

            if (activePointers.size === 2) {
                panning = false;
                pinching = true;
                pinchStartDist = pinchDistance();
                pinchStartZoom = zoom;
                return;
            }

            if (activePointers.size > 2) {
                return;
            }
        }

        if (e.button !== 0 || pinching) {
            return;
        }
        panning = true;
        moved = false;
        startX = e.clientX;
        startY = e.clientY;
        startScrollLeft = wrapEl.scrollLeft;
        startScrollTop = wrapEl.scrollTop;
        safeCapture(wrapEl, e.pointerId);
    });

    wrapEl.addEventListener('pointermove', (e) => {
        if (e.pointerType === 'touch' && activePointers.has(e.pointerId)) {
            activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
        }

        if (pinching && activePointers.size === 2) {
            const dist = pinchDistance();
            if (dist > 0 && pinchStartDist > 0) {
                const mid = pinchMidpoint();
                applyZoom(pinchStartZoom * (dist / pinchStartDist), mid.x, mid.y);
            }
            return;
        }

        if (!panning) {
            return;
        }
        const dx = e.clientX - startX;
        const dy = e.clientY - startY;
        if (!moved) {
            if (Math.sqrt(dx * dx + dy * dy) < dragThreshold) {
                return;
            }
            moved = true;
            wrapEl.style.cursor = 'grabbing';
        }
        wrapEl.scrollLeft = startScrollLeft - dx;
        wrapEl.scrollTop = startScrollTop - dy;
    });

    function endPointer(e) {
        if (e.pointerType === 'touch') {
            activePointers.delete(e.pointerId);
            safeRelease(wrapEl, e.pointerId);
            if (activePointers.size < 2) {
                pinching = false;
            }
            if (activePointers.size > 0) {
                return;
            }
        }

        if (!panning) {
            return;
        }
        panning = false;
        safeRelease(wrapEl, e.pointerId);
        wrapEl.style.cursor = '';

        if (moved) {
            suppressClick = true;
        }
    }

    wrapEl.addEventListener('pointerup', endPointer);
    wrapEl.addEventListener('pointercancel', endPointer);
}
// ===================================================================
// Felles geometri for streker og tilkoblingspunkter. Alle punkter er i
// prosent av lerretet (samme som PunkterJson), slik at skjemaet skalerer.
// ===================================================================

const SVG_NS = 'http://www.w3.org/2000/svg';
const EPS = 0.05;
const PORT_TREFF_PCT = 0.35;
const SNAP_RADIUS_PX = 14;
const STREK_HIT_PX = 8;
const UI_FARGE = '#835e41';
const PORT_FARGE = '#2457c5';
const GUIDE_FARGE = '#e0458b';

function lesPunkter(pl) {
    return (pl.getAttribute('points') || '').trim().split(/\s+/).filter(Boolean).map(par => {
        const [x, y] = par.split(',').map(Number);
        return { x, y };
    });
}

function punkterTilStr(pts) {
    return pts.map(p => `${+p.x.toFixed(3)},${+p.y.toFixed(3)}`).join(' ');
}

function punkterTilJson(pts) {
    return JSON.stringify(pts.map(p => ({ X: +p.x.toFixed(3), Y: +p.y.toFixed(3) })));
}

function lik(a, b) {
    return Math.abs(a - b) < EPS;
}

// Fjerner like punkter etter hverandre og midtpunkter på en rett linje,
// slik at en strek aldri får "usynlige" knekkpunkter.
function rens(pts) {
    const r = [];
    for (const p of pts) {
        const l = r[r.length - 1];
        if (l && lik(l.x, p.x) && lik(l.y, p.y)) {
            continue;
        }
        r.push({ x: p.x, y: p.y });
    }
    for (let i = r.length - 2; i > 0; i--) {
        const a = r[i - 1], b = r[i], c = r[i + 1];
        if ((lik(a.x, b.x) && lik(b.x, c.x)) || (lik(a.y, b.y) && lik(b.y, c.y))) {
            r.splice(i, 1);
        }
    }
    return r;
}

// Setter inn et hjørne i alle skrå strekbiter, slik at streken bare går
// vannrett og loddrett.
function rettOpp(pts) {
    const r = [];
    pts.forEach((p, i) => {
        const forrige = r[r.length - 1];
        if (i > 0 && forrige && !lik(forrige.x, p.x) && !lik(forrige.y, p.y)) {
            r.push({ x: p.x, y: forrige.y });
        }
        r.push({ x: p.x, y: p.y });
    });
    return rens(r);
}

function canvasRect(canvasEl) {
    return canvasEl.getBoundingClientRect();
}

function tilPx(rect, p) {
    return { x: (p.x / 100) * rect.width, y: (p.y / 100) * rect.height };
}

function fraEvent(canvasEl, e) {
    return getClickPercent(canvasEl, e.clientX, e.clientY);
}

function snapPct(canvasEl, v) {
    return canvasEl.dataset.snap === '1' ? Math.round(v * 2) / 2 : v;
}

function markerBoks(m) {
    return {
        x: parseFloat(m.style.left) || 0,
        y: parseFloat(m.style.top) || 0,
        w: parseFloat(m.style.width) || 0,
        h: parseFloat(m.style.height) || 0
    };
}

// Fire tilkoblingspunkter (midt på hver kant) per komponent. Punkt-figurer
// har bare ett - i midten.
function porterForBoks(b, erPunkt) {
    const cx = b.x + b.w / 2;
    const cy = b.y + b.h / 2;
    if (erPunkt) {
        return [{ side: 'c', x: cx, y: cy }];
    }
    return [
        { side: 't', x: cx, y: b.y },
        { side: 'r', x: b.x + b.w, y: cy },
        { side: 'b', x: cx, y: b.y + b.h },
        { side: 'l', x: b.x, y: cy }
    ];
}

// Egne tilkoblingspunkter (data-porter = [[x,y],...] i andel av symbolets
// størrelse). Finnes de, brukes bare de - ellers de vanlige punktene.
function porterForSymbol(el, b) {
    let egne = [];
    try {
        egne = JSON.parse(el.dataset.porter || '[]');
    } catch {
        egne = [];
    }
    if (Array.isArray(egne) && egne.length) {
        return egne.map((p, i) => ({ side: 'e' + i, x: b.x + p[0] * b.w, y: b.y + p[1] * b.h }));
    }
    return porterForBoks(b, el.dataset.elementtype === 'Punkt');
}

function porterForMarker(m) {
    if (m.dataset.elementtype === 'Tekstboks') {
        return [];
    }
    return porterForSymbol(m, markerBoks(m));
}

function allePorter(canvasEl) {
    const porter = [];
    canvasEl.querySelectorAll('.kobling-symbol-marker[data-symbolid]').forEach(m => {
        porterForMarker(m).forEach(p => porter.push(p));
    });
    // Endene på andre streker kan også kobles til (forgreninger).
    canvasEl.querySelectorAll('.kobling-strek-svg-overlay polyline[data-strekid]').forEach(pl => {
        const pts = lesPunkter(pl);
        if (pts.length > 1) {
            porter.push({ side: 'c', x: pts[0].x, y: pts[0].y });
            porter.push({ side: 'c', x: pts[pts.length - 1].x, y: pts[pts.length - 1].y });
        }
    });
    return porter;
}

function finnPortNaer(canvasEl, clientX, clientY) {
    const rect = canvasRect(canvasEl);
    let best = null;
    let bestDist = SNAP_RADIUS_PX;
    for (const p of allePorter(canvasEl)) {
        const px = rect.left + (p.x / 100) * rect.width;
        const py = rect.top + (p.y / 100) * rect.height;
        const d = Math.hypot(clientX - px, clientY - py);
        if (d <= bestDist) {
            bestDist = d;
            best = p;
        }
    }
    return best;
}

function avstandTilLinjestykke(p, a, b) {
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    const lengdeKvadrat = dx * dx + dy * dy;
    if (lengdeKvadrat === 0) {
        return Math.hypot(p.x - a.x, p.y - a.y);
    }
    let t = ((p.x - a.x) * dx + (p.y - a.y) * dy) / lengdeKvadrat;
    t = Math.max(0, Math.min(1, t));
    return Math.hypot(p.x - (a.x + t * dx), p.y - (a.y + t * dy));
}

function finnStrekNaer(canvasEl, clientX, clientY, kunLedere = false) {
    const rect = canvasRect(canvasEl);
    let best = null;
    let bestDist = STREK_HIT_PX;
    canvasEl.querySelectorAll('.kobling-strek-svg-overlay polyline[data-strekid]').forEach(pl => {
        if (kunLedere && !pl.dataset.strekid.startsWith('-')) {
            return;
        }
        const pts = lesPunkter(pl).map(p => ({
            x: rect.left + (p.x / 100) * rect.width,
            y: rect.top + (p.y / 100) * rect.height
        }));
        for (let i = 0; i < pts.length - 1; i++) {
            const d = avstandTilLinjestykke({ x: clientX, y: clientY }, pts[i], pts[i + 1]);
            if (d <= bestDist) {
                bestDist = d;
                best = parseInt(pl.dataset.strekid, 10);
            }
        }
    });
    return best;
}

function strekPolyline(canvasEl, id) {
    return canvasEl.querySelector(`.kobling-strek-svg-overlay polyline[data-strekid="${id}"]`);
}

// ---------- UI-laget (knekkpunkter, tilkoblingspunkter, hjelpelinjer) ----------
// Et tomt <svg class="ks-ui-lag"> rendres av Blazor; innholdet eies av JS.

function uiLag(canvasEl) {
    return canvasEl.querySelector('.ks-ui-lag');
}

function tomUi(canvasEl) {
    const ui = uiLag(canvasEl);
    if (ui) {
        ui.replaceChildren();
    }
    return ui;
}

function svgEl(navn, attr, forelder) {
    const el = document.createElementNS(SVG_NS, navn);
    for (const k in attr) {
        el.setAttribute(k, attr[k]);
    }
    if (forelder) {
        forelder.appendChild(el);
    }
    return el;
}

function tegnPorter(ui, rect, porter, aktiv) {
    for (const p of porter) {
        const q = tilPx(rect, p);
        const erAktiv = aktiv && lik(aktiv.x, p.x) && lik(aktiv.y, p.y);
        svgEl('circle', {
            cx: q.x, cy: q.y, r: erAktiv ? 7 : 4.5,
            fill: erAktiv ? PORT_FARGE : '#fff', stroke: PORT_FARGE, 'stroke-width': 1.5,
            opacity: erAktiv ? 1 : 0.85
        }, ui);
    }
}

function tegnPolyline(ui, rect, pts, farge, tykkelse, stiplet) {
    if (pts.length < 2) {
        return;
    }
    svgEl('polyline', {
        points: pts.map(p => { const q = tilPx(rect, p); return `${q.x},${q.y}`; }).join(' '),
        fill: 'none', stroke: farge, 'stroke-width': tykkelse,
        'stroke-linejoin': 'round', 'stroke-linecap': 'round',
        'stroke-dasharray': stiplet ? '6,4' : 'none'
    }, ui);
}

// ===================================================================
// Tegn strek: klikk for å starte (gjerne på et blått tilkoblingspunkt),
// klikk der streken skal svinge, og klikk på et tilkoblingspunkt for å
// koble. Dobbeltklikk eller Enter stopper streken der du er. Streken går
// alltid vannrett/loddrett - hold Shift for fri vinkel.
// ===================================================================

let currentStrekTegning = null;

export function startStrekTegning(canvasEl, dotNetRef, farge, tykkelse, stiplet, opts) {
    stopStrekTegning();

    // opts (valgfritt): { startPts, ferdig(pts), avbrutt() } - brukes når en
    // leder i en kabel forlenges fra der den slutter i dag.
    let pts = opts && opts.startPts ? opts.startPts.map(p => ({ x: p.x, y: p.y })) : [];
    const minPts = pts.length;
    let fullfort = false;
    let mus = null;
    let fri = false;
    let malPort = null;

    function aksePunkt(sist, raw) {
        const rect = canvasRect(canvasEl);
        const dx = ((raw.x - sist.x) / 100) * rect.width;
        const dy = ((raw.y - sist.y) / 100) * rect.height;
        return Math.abs(dx) >= Math.abs(dy) ? { x: raw.x, y: sist.y } : { x: sist.x, y: raw.y };
    }

    // Siste bit inn til et tilkoblingspunkt: kommer inn vinkelrett på
    // kanten (vannrett inn på venstre/høyre side, loddrett inn på topp/bunn).
    function veiTilPort(sist, port) {
        if (lik(sist.x, port.x) || lik(sist.y, port.y)) {
            return [{ x: port.x, y: port.y }];
        }
        const hjorne = port.side === 'l' || port.side === 'r'
            ? { x: sist.x, y: port.y }
            : { x: port.x, y: sist.y };
        return [hjorne, { x: port.x, y: port.y }];
    }

    function forhand() {
        if (!pts.length || !mus) {
            return [];
        }
        const sist = pts[pts.length - 1];
        if (malPort) {
            return fri ? [{ x: malPort.x, y: malPort.y }] : veiTilPort(sist, malPort);
        }
        return [fri ? mus : aksePunkt(sist, mus)];
    }

    function tegnUi() {
        const ui = tomUi(canvasEl);
        if (!ui) {
            return;
        }
        const rect = canvasRect(canvasEl);
        tegnPorter(ui, rect, allePorter(canvasEl), malPort);
        if (pts.length) {
            tegnPolyline(ui, rect, pts, farge, tykkelse, stiplet);
            const f = forhand();
            if (f.length) {
                tegnPolyline(ui, rect, [pts[pts.length - 1], ...f], farge, tykkelse, true);
            }
            pts.forEach(p => {
                const q = tilPx(rect, p);
                svgEl('circle', { cx: q.x, cy: q.y, r: 3.5, fill: '#fff', stroke: farge, 'stroke-width': 1.5 }, ui);
            });
        }
    }

    function ferdig() {
        const r = rens(pts);
        pts = [];
        tegnUi();
        if (opts) {
            fullfort = true;
            if (r.length >= 2 && opts.ferdig) {
                opts.ferdig(r);
            }
            stopStrekTegning();
            return;
        }
        if (r.length >= 2) {
            dotNetRef.invokeMethodAsync('OnStrekFerdig', punkterTilJson(r));
        }
    }

    function rawFra(e) {
        const p = fraEvent(canvasEl, e);
        return { x: snapPct(canvasEl, p.x), y: snapPct(canvasEl, p.y) };
    }

    function onPointerDown(e) {
        if (e.button !== 0) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();

        malPort = finnPortNaer(canvasEl, e.clientX, e.clientY);
        fri = e.shiftKey;
        mus = malPort ? { x: malPort.x, y: malPort.y } : rawFra(e);

        if (!pts.length) {
            pts = [{ x: mus.x, y: mus.y }];
            malPort = null;
            tegnUi();
            return;
        }

        const kobletTilPort = !!malPort && pts.length > 0;
        pts = rens([...pts, ...forhand()]);
        if (kobletTilPort && pts.length >= 2) {
            ferdig();
            return;
        }
        tegnUi();
    }

    function onPointerMove(e) {
        malPort = finnPortNaer(canvasEl, e.clientX, e.clientY);
        // Ikke snap til punktet streken akkurat startet i.
        if (malPort && pts.length && lik(malPort.x, pts[0].x) && lik(malPort.y, pts[0].y) && pts.length < 2) {
            malPort = null;
        }
        fri = e.shiftKey;
        mus = rawFra(e);
        tegnUi();
    }

    function onDblClick(e) {
        e.preventDefault();
        e.stopPropagation();
        if (pts.length >= 2) {
            ferdig();
        }
    }

    function onKeyDown(e) {
        const tag = (e.target.tagName || '').toLowerCase();
        if (tag === 'input' || tag === 'textarea' || tag === 'select') {
            return;
        }
        if (e.key === 'Escape') {
            e.preventDefault();
            if (pts.length >= 2) {
                ferdig();
            } else if (pts.length) {
                pts = [];
                tegnUi();
            } else {
                dotNetRef.invokeMethodAsync('OnSnarvei', 'velg');
            }
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (pts.length >= 2) {
                ferdig();
            }
        } else if (e.key === 'Backspace' && pts.length > minPts) {
            e.preventDefault();
            pts.pop();
            tegnUi();
        }
    }

    function onLeave() {
        mus = null;
        malPort = null;
        tegnUi();
    }

    function cleanup() {
        canvasEl.removeEventListener('pointerdown', onPointerDown, true);
        canvasEl.removeEventListener('pointermove', onPointerMove, true);
        canvasEl.removeEventListener('pointerleave', onLeave);
        canvasEl.removeEventListener('dblclick', onDblClick, true);
        document.removeEventListener('keydown', onKeyDown);

        tomUi(canvasEl);
        if (currentStrekTegning === api) {
            currentStrekTegning = null;
        }
        if (opts && !fullfort && opts.avbrutt) {
            fullfort = true;
            opts.avbrutt();
        }
    }

    // Capture-fase: et klikk oppå et bilde eller en komponent blir en sving i
    // streken i stedet for å velge/dra komponenten mens man tegner.
    canvasEl.addEventListener('pointerdown', onPointerDown, true);
    canvasEl.addEventListener('pointermove', onPointerMove, true);
    canvasEl.addEventListener('pointerleave', onLeave);
    canvasEl.addEventListener('dblclick', onDblClick, true);
    document.addEventListener('keydown', onKeyDown);


    const api = { cleanup, ferdig: () => { if (pts.length >= 2) { ferdig(); } } };
    currentStrekTegning = api;
    tegnUi();
}

export function stopStrekTegning() {
    if (currentStrekTegning) {
        currentStrekTegning.cleanup();
        currentStrekTegning = null;
    }
}

export function fullforStrek() {
    if (currentStrekTegning) {
        currentStrekTegning.ferdig();
    }
}

// ===================================================================
// Redigere en valgt strek: dra knekkpunktene for å flytte svingene, dra
// den lille prikken midt på en strekbit for å flytte biten parallelt, og
// dobbeltklikk et knekkpunkt for å fjerne svingen. Endene kan dras til et
// annet tilkoblingspunkt.
// ===================================================================

function tegnStrekHandtak(canvasEl) {
    if (currentStrekTegning) {
        return;
    }
    const ui = tomUi(canvasEl);
    const id = parseInt(canvasEl.dataset.valgtstrek || '0', 10);
    if (!ui || !id || canvasEl.dataset.locked === '1') {
        return;
    }
    const pl = strekPolyline(canvasEl, id);
    if (!pl) {
        return;
    }
    const rect = canvasRect(canvasEl);
    const pts = lesPunkter(pl);

    // Hver strekbit kan gripes hvor som helst langs biten (bred, usynlig
    // treffsone), og flyttes da parallelt. Prikken midt på viser at det går.
    for (let i = 0; i < pts.length - 1; i++) {
        const a = tilPx(rect, pts[i]);
        const b = tilPx(rect, pts[i + 1]);
        const treff = svgEl('line', {
            x1: a.x, y1: a.y, x2: b.x, y2: b.y,
            stroke: 'transparent', 'stroke-width': 16, 'stroke-linecap': 'round', 'data-h': 'm', 'data-i': i
        }, ui);
        treff.style.pointerEvents = 'stroke';
        treff.style.cursor = lik(pts[i].y, pts[i + 1].y) ? 'ns-resize' : lik(pts[i].x, pts[i + 1].x) ? 'ew-resize' : 'move';
        svgEl('title', {}, treff).textContent = 'Dra for å flytte denne biten';
        if (Math.hypot(b.x - a.x, b.y - a.y) >= 26) {
            svgEl('circle', { cx: (a.x + b.x) / 2, cy: (a.y + b.y) / 2, r: 4, fill: UI_FARGE, opacity: 0.6 }, ui);
        }
    }

    pts.forEach((p, i) => {
        const q = tilPx(rect, p);
        const ende = i === 0 || i === pts.length - 1;
        if (ende) {
            svgEl('rect', { x: q.x - 5, y: q.y - 5, width: 10, height: 10, rx: 2, fill: '#fff', stroke: UI_FARGE, 'stroke-width': 2 }, ui);
        } else {
            svgEl('circle', { cx: q.x, cy: q.y, r: 6, fill: '#fff', stroke: UI_FARGE, 'stroke-width': 2 }, ui);
        }
        const treff = svgEl('circle', { cx: q.x, cy: q.y, r: 11, fill: 'transparent', 'data-h': 'v', 'data-i': i }, ui);
        treff.style.pointerEvents = 'all';
        treff.style.cursor = 'move';
        svgEl('title', {}, treff).textContent = ende ? 'Dra enden til et tilkoblingspunkt' : 'Dra for å flytte svingen · dobbeltklikk for å fjerne den';
    });
}

export function oppdaterEditorUi(canvasEl, valgtStrekId, snap) {
    canvasEl.dataset.valgtstrek = String(valgtStrekId || 0);
    canvasEl.dataset.snap = snap ? '1' : '0';
    if (!currentStrekTegning) {
        tegnStrekHandtak(canvasEl);
    }
}

export function initStrekRedigering(canvasEl, dotNetRef) {
    if (canvasEl.dataset.strekRedBound === '1') {
        return;
    }
    canvasEl.dataset.strekRedBound = '1';

    let drag = null;

    function flyttKnekk(orig, i, q) {
        const arr = orig.map(p => ({ ...p }));
        const siste = arr.length - 1;
        arr[i] = { x: q.x, y: q.y };
        for (const n of [i - 1, i + 1]) {
            if (n < 0 || n > siste) {
                continue;
            }
            const erEnde = n === 0 || n === siste;
            if (erEnde) {
                continue;
            }
            if (lik(orig[n].x, orig[i].x)) {
                arr[n].x = q.x;
            } else if (lik(orig[n].y, orig[i].y)) {
                arr[n].y = q.y;
            }
        }
        return drag.fri ? arr : rettOpp(arr);
    }

    function flyttBit(orig, i, q) {
        const arr = orig.map(p => ({ ...p }));
        let s = i;
        let e = i + 1;
        const vannrett = lik(arr[s].y, arr[e].y);
        const loddrett = lik(arr[s].x, arr[e].x);
        if (!vannrett && !loddrett) {
            const dx = q.x - drag.start.x;
            const dy = q.y - drag.start.y;
            arr[s] = { x: orig[s].x + dx, y: orig[s].y + dy };
            arr[e] = { x: orig[e].x + dx, y: orig[e].y + dy };
            return arr;
        }
        // Endene skal bli stående der de er koblet - legg inn en ekstra
        // sving i stedet for å dra endepunktet med.
        if (s === 0) {
            arr.unshift({ ...arr[0] });
            s++;
            e++;
        }
        if (e === arr.length - 1) {
            arr.push({ ...arr[arr.length - 1] });
        }
        if (vannrett) {
            arr[s].y = q.y;
            arr[e].y = q.y;
        } else {
            arr[s].x = q.x;
            arr[e].x = q.x;
        }
        return arr;
    }

    canvasEl.addEventListener('pointerdown', (e) => {
        if (e.button !== 0 || canvasEl.dataset.locked === '1' || currentStrekTegning) {
            return;
        }
        const valgtId = parseInt(canvasEl.dataset.valgtstrek || '0', 10);
        const h = e.target.closest && e.target.closest('[data-h]');

        if (h && valgtId) {
            const pl = strekPolyline(canvasEl, valgtId);
            if (!pl) {
                return;
            }
            e.preventDefault();
            e.stopImmediatePropagation();
            const p = fraEvent(canvasEl, e);
            drag = {
                type: h.dataset.h, i: parseInt(h.dataset.i, 10), pl, id: valgtId,
                orig: lesPunkter(pl), start: p, flyttet: false, fri: e.shiftKey, resultat: null
            };
            safeCapture(canvasEl, e.pointerId);
            return;
        }

        if (e.target.closest && e.target.closest('.kobling-symbol-marker')) {
            return;
        }

        // Lederne ligger oppå enden av kabelen - de går foran når klikket er nær en leder.
        const lederId = finnStrekNaer(canvasEl, e.clientX, e.clientY, true);
        const strekId = lederId ?? finnStrekNaer(canvasEl, e.clientX, e.clientY);
        if (strekId !== null && strekId < 0) {
            // En leder i en kabel: den følger musen videre fra enden. Klikk lager
            // sving, klikk på et tilkoblingspunkt kobler, Esc stopper.
            e.preventDefault();
            e.stopImmediatePropagation();
            startLederForlengelse(canvasEl, dotNetRef, strekId);
            return;
        }
        if (strekId !== null) {
            e.preventDefault();
            e.stopImmediatePropagation();
            if (strekId !== valgtId) {
                canvasEl.dataset.valgtstrek = String(strekId);
                tegnStrekHandtak(canvasEl);
                dotNetRef.invokeMethodAsync('OnStrekValgt', strekId);
            }
            // Man kan gripe streken og dra biten med en gang.
            const pl = strekPolyline(canvasEl, strekId);
            const orig = lesPunkter(pl);
            const rect = canvasRect(canvasEl);
            const m = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            let bestI = 0;
            let bestD = Infinity;
            for (let i = 0; i < orig.length - 1; i++) {
                const d = avstandTilLinjestykke(m, tilPx(rect, orig[i]), tilPx(rect, orig[i + 1]));
                if (d < bestD) {
                    bestD = d;
                    bestI = i;
                }
            }
            drag = {
                type: 'm', i: bestI, pl, id: strekId, orig,
                start: fraEvent(canvasEl, e), flyttet: false, fri: e.shiftKey, resultat: null
            };
            safeCapture(canvasEl, e.pointerId);
            return;
        }

        if (valgtId) {
            canvasEl.dataset.valgtstrek = '0';
            tomUi(canvasEl);
            dotNetRef.invokeMethodAsync('OnStrekValgt', 0);
        }
    }, true);

    canvasEl.addEventListener('pointermove', (e) => {
        if (!drag) {
            return;
        }
        const raw = fraEvent(canvasEl, e);
        if (!drag.flyttet && Math.hypot(raw.x - drag.start.x, raw.y - drag.start.y) < 0.3) {
            return;
        }
        drag.flyttet = true;
        drag.fri = e.shiftKey;
        let q = { x: snapPct(canvasEl, raw.x), y: snapPct(canvasEl, raw.y) };
        const siste = drag.orig.length - 1;
        let arr;
        if (drag.type === 'v') {
            if (drag.i === 0 || drag.i === siste) {
                const port = finnPortNaer(canvasEl, e.clientX, e.clientY);
                if (port) {
                    q = { x: port.x, y: port.y };
                }
            }
            arr = flyttKnekk(drag.orig, drag.i, q);
        } else {
            arr = flyttBit(drag.orig, drag.i, q);
        }
        drag.resultat = rens(arr);
        drag.pl.setAttribute('points', punkterTilStr(drag.resultat));
        tegnStrekHandtak(canvasEl);
    });

    function slutt(e) {
        if (!drag) {
            return;
        }
        safeRelease(canvasEl, e.pointerId);
        const d = drag;
        drag = null;
        if (d.flyttet && d.resultat && d.resultat.length >= 2) {
            dotNetRef.invokeMethodAsync('OnStrekFlyttet', d.id, punkterTilJson(d.resultat));
        }
    }

    canvasEl.addEventListener('pointerup', slutt);
    canvasEl.addEventListener('pointercancel', slutt);

    canvasEl.addEventListener('dblclick', (e) => {
        if (canvasEl.dataset.locked === '1' || currentStrekTegning) {
            return;
        }
        const h = e.target.closest && e.target.closest('[data-h="v"]');
        const valgtId = parseInt(canvasEl.dataset.valgtstrek || '0', 10);
        if (!h || !valgtId) {
            return;
        }
        const pl = strekPolyline(canvasEl, valgtId);
        const pts = lesPunkter(pl);
        const i = parseInt(h.dataset.i, 10);
        if (i === 0 || i === pts.length - 1 || pts.length <= 2) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        pts.splice(i, 1);
        const ny = rettOpp(pts);
        pl.setAttribute('points', punkterTilStr(ny));
        tegnStrekHandtak(canvasEl);
        dotNetRef.invokeMethodAsync('OnStrekFlyttet', valgtId, punkterTilJson(ny));
    });

    // Tegn håndtakene på nytt når lerretet endrer størrelse (zoom/vindu).
    if (window.ResizeObserver) {
        new ResizeObserver(() => tegnStrekHandtak(canvasEl)).observe(canvasEl);
    }
}

// ===================================================================
// Streker som følger med komponentene. Når en komponent flyttes eller
// endrer størrelse, flyttes strek-ender som sitter på et av komponentens
// tilkoblingspunkter med, og nabopunktet justeres så streken holder seg rett.
// ===================================================================

function finnTilkoblinger(canvasEl, grupper) {
    const res = [];
    canvasEl.querySelectorAll('.kobling-strek-svg-overlay polyline[data-strekid]').forEach(pl => {
        const orig = lesPunkter(pl);
        if (orig.length < 2) {
            return;
        }
        const ender = [];
        for (const idx of [0, orig.length - 1]) {
            for (const g of grupper) {
                const port = porterForSymbol(g.el, g.startBoks)
                    .find(p => Math.abs(p.x - orig[idx].x) < PORT_TREFF_PCT && Math.abs(p.y - orig[idx].y) < PORT_TREFF_PCT);
                if (port) {
                    ender.push({ idx, g, side: port.side });
                    break;
                }
            }
        }
        if (ender.length) {
            res.push({ pl, id: parseInt(pl.dataset.strekid, 10), orig, ender });
        }
    });
    return res;
}

function nyPortPos(g, side) {
    const b = markerBoks(g.el);
    return porterForSymbol(g.el, b).find(p => p.side === side) ?? porterForBoks(b, true)[0];
}

function oppdaterTilkobledeStreker(tilkoblinger) {
    for (const t of tilkoblinger) {
        const siste = t.orig.length - 1;
        // Begge endene på samme flytting: flytt hele streken like mye.
        if (t.ender.length === 2) {
            const a = nyPortPos(t.ender[0].g, t.ender[0].side);
            const b = nyPortPos(t.ender[1].g, t.ender[1].side);
            const da = { x: a.x - t.orig[t.ender[0].idx].x, y: a.y - t.orig[t.ender[0].idx].y };
            const db = { x: b.x - t.orig[t.ender[1].idx].x, y: b.y - t.orig[t.ender[1].idx].y };
            if (lik(da.x, db.x) && lik(da.y, db.y)) {
                t.resultat = t.orig.map(p => ({ x: p.x + da.x, y: p.y + da.y }));
                t.pl.setAttribute('points', punkterTilStr(t.resultat));
                continue;
            }
        }
        const arr = t.orig.map(p => ({ ...p }));
        for (const ende of t.ender) {
            const ny = nyPortPos(ende.g, ende.side);
            const gml = t.orig[ende.idx];
            arr[ende.idx] = { x: ny.x, y: ny.y };
            const n = ende.idx === 0 ? 1 : siste - 1;
            if (arr.length > 2 && n > 0 && n < siste) {
                if (lik(t.orig[n].x, gml.x)) {
                    arr[n].x = ny.x;
                } else if (lik(t.orig[n].y, gml.y)) {
                    arr[n].y = ny.y;
                }
            }
        }
        t.resultat = rettOpp(arr);
        t.pl.setAttribute('points', punkterTilStr(t.resultat));
    }
}

function tilkoblingerTilJson(tilkoblinger) {
    return JSON.stringify(tilkoblinger
        .filter(t => t.resultat && t.resultat.length >= 2)
        .map(t => ({ Id: t.id, PunkterJson: punkterTilJson(rens(t.resultat)) })));
}

// Rosa hjelpelinjer + smart snapping når en komponent står på linje med en annen.
function justerMotAndre(canvasEl, el) {
    const ui = uiLag(canvasEl);
    if (!ui) {
        return;
    }
    ui.querySelectorAll('.ks-guide').forEach(g => g.remove());
    const rect = canvasRect(canvasEl);
    const b = markerBoks(el);
    let cx = b.x + b.w / 2;
    let cy = b.y + b.h / 2;
    const terskelX = (6 / rect.width) * 100;
    const terskelY = (6 / rect.height) * 100;
    let snapX = null;
    let snapY = null;

    canvasEl.querySelectorAll('.kobling-symbol-marker[data-symbolid]').forEach(o => {
        if (o === el) {
            return;
        }
        const ob = markerBoks(o);
        const ox = ob.x + ob.w / 2;
        const oy = ob.y + ob.h / 2;
        if (snapX === null && Math.abs(ox - cx) < terskelX) {
            snapX = { v: ox, o: ob };
        }
        if (snapY === null && Math.abs(oy - cy) < terskelY) {
            snapY = { v: oy, o: ob };
        }
    });

    if (snapX) {
        b.x = snapX.v - b.w / 2;
        el.style.left = b.x + '%';
        cx = snapX.v;
        const y1 = Math.min(snapX.o.y, b.y) - 2;
        const y2 = Math.max(snapX.o.y + snapX.o.h, b.y + b.h) + 2;
        const a = tilPx(rect, { x: cx, y: y1 });
        const c = tilPx(rect, { x: cx, y: y2 });
        svgEl('line', { x1: a.x, y1: a.y, x2: c.x, y2: c.y, stroke: GUIDE_FARGE, 'stroke-width': 1, 'stroke-dasharray': '4 3', class: 'ks-guide' }, ui);
    }
    if (snapY) {
        b.y = snapY.v - b.h / 2;
        el.style.top = b.y + '%';
        cy = snapY.v;
        const x1 = Math.min(snapY.o.x, b.x) - 2;
        const x2 = Math.max(snapY.o.x + snapY.o.w, b.x + b.w) + 2;
        const a = tilPx(rect, { x: x1, y: cy });
        const c = tilPx(rect, { x: x2, y: cy });
        svgEl('line', { x1: a.x, y1: a.y, x2: c.x, y2: c.y, stroke: GUIDE_FARGE, 'stroke-width': 1, 'stroke-dasharray': '4 3', class: 'ks-guide' }, ui);
    }
    el.dataset.x = cx;
    el.dataset.y = cy;
}

function fjernGuider(canvasEl) {
    const ui = uiLag(canvasEl);
    if (ui) {
        ui.querySelectorAll('.ks-guide').forEach(g => g.remove());
    }
}

const GRID_STEP = 2;

function snapToGrid(containerEl, value) {
    if (containerEl.dataset.snap === '0') {
        return Math.round(value * 10) / 10;
    }
    return Math.round(value / GRID_STEP) * GRID_STEP;
}

export function attachSymbolMarkers(containerEl, dotNetRef, locked) {
    containerEl.dataset.locked = locked ? '1' : '0';
    if (locked && !currentStrekTegning) {
        tomUi(containerEl);
    }

    const markers = containerEl.querySelectorAll('.kobling-symbol-marker[data-symbolid]');

    markers.forEach(markerEl => {
        const symbolId = parseInt(markerEl.dataset.symbolid, 10);

        if (markerEl.dataset.dragBound !== '1') {
            markerEl.dataset.dragBound = '1';

            const dragThreshold = 4;
            let dragging = false;
            let moved = false;
            let canDrag = false;
            let startX = 0;
            let startY = 0;
            let groupMarkers = [];
            let tilkoblinger = [];

            markerEl.addEventListener('click', (e) => {
                e.stopPropagation();
            });

            markerEl.addEventListener('pointerdown', (e) => {
                if (e.button !== 0 || e.target.closest('.kobling-resize-handle') || containerEl.dataset.locked === '1') {
                    return;
                }
                e.preventDefault();
                e.stopPropagation();
                dragging = true;
                moved = false;
                startX = e.clientX;
                startY = e.clientY;
                canDrag = markerEl.dataset.laast !== 'true';

                if (canDrag) {
                    const isSelected = markerEl.classList.contains('kobling-symbol-marker-selected');
                    const gruppe = isSelected
                        ? Array.from(containerEl.querySelectorAll('.kobling-symbol-marker-selected'))
                        : [markerEl];
                    groupMarkers = gruppe
                        .filter(el => el.dataset.laast !== 'true')
                        .map(el => ({
                            el,
                            startLeft: parseFloat(el.style.left) || 0,
                            startTop: parseFloat(el.style.top) || 0,
                            startBoks: markerBoks(el)
                        }));
                } else {
                    groupMarkers = [];
                }
                tilkoblinger = [];

                safeCapture(markerEl, e.pointerId);
            });

            markerEl.addEventListener('pointermove', (e) => {
                if (!dragging || !canDrag || containerEl.dataset.locked === '1') {
                    return;
                }
                const dx = e.clientX - startX;
                const dy = e.clientY - startY;
                if (!moved) {
                    if (Math.sqrt(dx * dx + dy * dy) < dragThreshold) {
                        return;
                    }
                    moved = true;
                    tilkoblinger = finnTilkoblinger(containerEl, groupMarkers);
                }
                const rect = containerEl.getBoundingClientRect();
                const pdx = (dx / rect.width) * 100;
                const pdy = (dy / rect.height) * 100;

                groupMarkers.forEach(g => {
                    const nx = snapToGrid(containerEl, Math.max(0, g.startLeft + pdx));
                    const ny = snapToGrid(containerEl, Math.max(0, g.startTop + pdy));
                    g.el.style.left = nx + '%';
                    g.el.style.top = ny + '%';
                    const w = parseFloat(g.el.style.width) || 0;
                    const h = parseFloat(g.el.style.height) || 0;
                    g.el.dataset.x = nx + w / 2;
                    g.el.dataset.y = ny + h / 2;
                });

                if (groupMarkers.length === 1) {
                    justerMotAndre(containerEl, groupMarkers[0].el);
                }
                oppdaterTilkobledeStreker(tilkoblinger);
            });

            markerEl.addEventListener('pointerup', (e) => {
                if (!dragging) {
                    return;
                }
                dragging = false;
                safeRelease(markerEl, e.pointerId);
                fjernGuider(containerEl);

                if (moved) {
                    const payload = groupMarkers.map(g => ({
                        Id: parseInt(g.el.dataset.symbolid, 10),
                        X: parseFloat(g.el.style.left),
                        Y: parseFloat(g.el.style.top)
                    }));
                    dotNetRef.invokeMethodAsync('OnSymbolerFlyttet', JSON.stringify(payload), tilkoblingerTilJson(tilkoblinger));
                } else {
                    dotNetRef.invokeMethodAsync('OnSymbolClicked', symbolId, e.shiftKey);
                }
                groupMarkers = [];
                tilkoblinger = [];
            });
        }

        const handleEl = markerEl.querySelector('.kobling-resize-handle');
        if (handleEl && handleEl.dataset.resizeBound !== '1') {
            handleEl.dataset.resizeBound = '1';

            let resizing = false;
            let startWidthPct = 0;
            let startHeightPct = 0;
            let startClientX = 0;
            let startClientY = 0;
            let tilkoblinger = [];

            handleEl.addEventListener('pointerdown', (e) => {
                if (e.button !== 0 || containerEl.dataset.locked === '1') {
                    return;
                }
                e.preventDefault();
                e.stopPropagation();
                resizing = true;
                startClientX = e.clientX;
                startClientY = e.clientY;
                startWidthPct = parseFloat(markerEl.style.width) || 10;
                startHeightPct = parseFloat(markerEl.style.height) || 10;
                tilkoblinger = finnTilkoblinger(containerEl, [{ el: markerEl, startBoks: markerBoks(markerEl) }]);
                safeCapture(handleEl, e.pointerId);
            });

            handleEl.addEventListener('pointermove', (e) => {
                if (!resizing) {
                    return;
                }
                const rect = containerEl.getBoundingClientRect();
                const dw = ((e.clientX - startClientX) / rect.width) * 100;
                const dh = ((e.clientY - startClientY) / rect.height) * 100;
                const newWidth = snapToGrid(containerEl, Math.max(2, startWidthPct + dw));
                const newHeight = snapToGrid(containerEl, Math.max(2, startHeightPct + dh));
                markerEl.style.width = newWidth + '%';
                markerEl.style.height = newHeight + '%';
                markerEl.dataset.w = newWidth;
                markerEl.dataset.h = newHeight;
                const left = parseFloat(markerEl.style.left) || 0;
                const top = parseFloat(markerEl.style.top) || 0;
                markerEl.dataset.x = left + newWidth / 2;
                markerEl.dataset.y = top + newHeight / 2;
                oppdaterTilkobledeStreker(tilkoblinger);
            });

            handleEl.addEventListener('pointerup', (e) => {
                if (!resizing) {
                    return;
                }
                resizing = false;
                safeRelease(handleEl, e.pointerId);
                const w = parseFloat(markerEl.dataset.w) || startWidthPct;
                const h = parseFloat(markerEl.dataset.h) || startHeightPct;
                dotNetRef.invokeMethodAsync('OnSymbolResized', symbolId, w, h, tilkoblingerTilJson(tilkoblinger));
                tilkoblinger = [];
            });
        }
    });
}

// Hurtigtaster for selve editoren: S = tegn strek, V = velg/flytt,
// T = tekst, Ctrl+Z / Ctrl+Y = angre / gjør om, Esc = fjern markering.
export function enableEditorSnarveier(containerEl, dotNetRef) {
    if (containerEl._editorSnarveier) {
        return;
    }
    const handler = (e) => {
        if (containerEl.dataset.rediger !== '1') {
            return;
        }
        const tag = (e.target.tagName || '').toLowerCase();
        if (tag === 'input' || tag === 'textarea' || tag === 'select' || e.target.isContentEditable) {
            return;
        }
        const ctrl = e.ctrlKey || e.metaKey;
        const tast = e.key.toLowerCase();
        let kommando = null;
        if (ctrl && tast === 'z') {
            kommando = e.shiftKey ? 'gjorom' : 'angre';
        } else if (ctrl && tast === 'y') {
            kommando = 'gjorom';
        } else if (!ctrl && !e.altKey) {
            if (tast === 's') {
                kommando = 'strek';
            } else if (tast === 'v') {
                kommando = 'velg';
            } else if (tast === 't') {
                kommando = 'tekst';
            } else if (e.key === 'Escape' && !currentStrekTegning) {
                kommando = 'avvelg';
            }
        }
        if (kommando) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnSnarvei', kommando);
        }
    };
    containerEl._editorSnarveier = handler;
    document.addEventListener('keydown', handler);
}

export function disableEditorSnarveier(containerEl) {
    if (containerEl._editorSnarveier) {
        document.removeEventListener('keydown', containerEl._editorSnarveier);
        containerEl._editorSnarveier = null;
    }
    stopStrekTegning();
}

const NUDGE_STEP = 1;
let nudgeSaveTimer = null;

export function enableKeyboardShortcuts(containerEl, dotNetRef) {
    if (containerEl.dataset.keysBound === '1') {
        return;
    }
    containerEl.dataset.keysBound = '1';

    // Lagret på selve elementet (ikke en anonym funksjon rett i
    // addEventListener) slik at disableKeyboardShortcuts kan fjerne NØYAKTIG
    // denne lytteren igjen - se CODE_REVIEW.md 2026-10-08 (bekreftet
    // minnelekkasje: lytteren ble tidligere aldri fjernet).
    const handler = (e) => {
        if (containerEl.dataset.locked === '1') {
            return;
        }

        const tag = (e.target.tagName || '').toLowerCase();
        if (tag === 'input' || tag === 'textarea' || tag === 'select') {
            return;
        }

        // Krever at fokus er inne på selve lerretet (eller ingenting spesifikt er fokusert),
        // slik at f.eks. Delete/piltaster ikke utløses ved siden-panelet, knapper eller andre steder på siden.
        const insideCanvas = containerEl.contains(document.activeElement);
        const nothingFocused = document.activeElement === document.body;
        if (!insideCanvas && !nothingFocused) {
            return;
        }

        const arrows = { ArrowUp: [0, -1], ArrowDown: [0, 1], ArrowLeft: [-1, 0], ArrowRight: [1, 0] };

        if (e.key === 'Delete') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnSlettTastetrykk');
            return;
        }

        const delta = arrows[e.key];
        if (!delta) {
            return;
        }

        const selected = containerEl.querySelectorAll('.kobling-symbol-marker-selected');
        if (selected.length !== 1 || selected[0].dataset.laast === 'true') {
            return;
        }

        e.preventDefault();
        const markerEl = selected[0];
        const left = Math.min(100, Math.max(0, (parseFloat(markerEl.style.left) || 0) + delta[0] * NUDGE_STEP));
        const top = Math.min(100, Math.max(0, (parseFloat(markerEl.style.top) || 0) + delta[1] * NUDGE_STEP));
        markerEl.style.left = left + '%';
        markerEl.style.top = top + '%';
        const w = parseFloat(markerEl.style.width) || 0;
        const h = parseFloat(markerEl.style.height) || 0;
        markerEl.dataset.x = left + w / 2;
        markerEl.dataset.y = top + h / 2;

        clearTimeout(nudgeSaveTimer);
        nudgeSaveTimer = setTimeout(() => {
            const symbolId = parseInt(markerEl.dataset.symbolid, 10);
            dotNetRef.invokeMethodAsync('OnSymbolMoved', symbolId, parseFloat(markerEl.style.left), parseFloat(markerEl.style.top));
        }, 400);
    };

    containerEl._keyboardShortcutsHandler = handler;
    document.addEventListener('keydown', handler);
}

export function disableKeyboardShortcuts(containerEl) {
    if (containerEl._keyboardShortcutsHandler) {
        document.removeEventListener('keydown', containerEl._keyboardShortcutsHandler);
        containerEl._keyboardShortcutsHandler = null;
    }
    containerEl.dataset.keysBound = '0';
    clearTimeout(nudgeSaveTimer);
}

export function enableRubberBandSelect(canvasEl, dotNetRef) {
    if (canvasEl.dataset.rubberBound === '1') {
        return;
    }
    canvasEl.dataset.rubberBound = '1';

    let box = null;
    let active = false;
    let startX = 0;
    let startY = 0;

    canvasEl.addEventListener('pointerdown', (e) => {
        if (e.button !== 0 || canvasEl.dataset.locked === '1') {
            return;
        }
        if (e.target !== canvasEl && !e.target.classList.contains('kobling-strek-svg-overlay')) {
            return;
        }

        active = true;
        startX = e.clientX;
        startY = e.clientY;
        box = document.createElement('div');
        box.className = 'kobling-rubber-band';
        canvasEl.appendChild(box);
        safeCapture(canvasEl, e.pointerId);
    });

    canvasEl.addEventListener('pointermove', (e) => {
        if (!active || !box) {
            return;
        }
        const rect = canvasEl.getBoundingClientRect();
        const x1 = Math.min(startX, e.clientX) - rect.left;
        const y1 = Math.min(startY, e.clientY) - rect.top;
        const x2 = Math.max(startX, e.clientX) - rect.left;
        const y2 = Math.max(startY, e.clientY) - rect.top;
        box.style.left = x1 + 'px';
        box.style.top = y1 + 'px';
        box.style.width = (x2 - x1) + 'px';
        box.style.height = (y2 - y1) + 'px';
    });

    canvasEl.addEventListener('pointerup', (e) => {
        if (!active) {
            return;
        }
        active = false;
        safeRelease(canvasEl, e.pointerId);

        if (!box) {
            return;
        }
        const boxRect = box.getBoundingClientRect();
        box.remove();
        box = null;

        if (boxRect.width < 4 || boxRect.height < 4) {
            return;
        }

        const markers = canvasEl.querySelectorAll('.kobling-symbol-marker[data-symbolid]');
        const matched = [];
        markers.forEach(m => {
            const r = m.getBoundingClientRect();
            const intersects = r.left < boxRect.right && r.right > boxRect.left && r.top < boxRect.bottom && r.bottom > boxRect.top;
            if (intersects) {
                matched.push(parseInt(m.dataset.symbolid, 10));
            }
        });

        dotNetRef.invokeMethodAsync('OnRubberBandValgt', JSON.stringify(matched), e.shiftKey);
    });

}

function startLederForlengelse(canvasEl, dotNetRef, lederId) {
    const pl = strekPolyline(canvasEl, lederId);
    if (!pl) {
        return;
    }
    const pts = lesPunkter(pl);
    if (pts.length < 2) {
        return;
    }
    canvasEl.dataset.valgtstrek = '0';
    tomUi(canvasEl);
    dotNetRef.invokeMethodAsync('OnLederTegningStart', lederId);
    startStrekTegning(canvasEl, dotNetRef, pl.getAttribute('stroke') || '#1d1d1b', 2, false, {
        startPts: pts,
        ferdig: r => dotNetRef.invokeMethodAsync('OnLederForlenget', lederId, punkterTilJson(r)),
        avbrutt: () => dotNetRef.invokeMethodAsync('OnLederTegningSlutt')
    });
}

// ===================================================================
// Egne koblingspunkter: klikk på den valgte komponenten for å legge inn et
// blått punkt der kablene skal kobles. Klikk utenfor eller Esc avslutter.
// ===================================================================
let stoppPort = null;

export function startPortPlassering(canvasEl, dotNetRef, symbolId) {
    stoppPortPlassering();

    function avslutt() {
        stoppPortPlassering();
        dotNetRef.invokeMethodAsync('OnPortPlasseringSlutt');
    }

    function ned(e) {
        if (e.button !== 0) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        const m = canvasEl.querySelector(`.kobling-symbol-marker[data-symbolid="${symbolId}"]`);
        const r = m ? m.getBoundingClientRect() : null;
        if (!r || e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom) {
            avslutt();
            return;
        }
        dotNetRef.invokeMethodAsync('OnPortLagtTil', symbolId, (e.clientX - r.left) / r.width, (e.clientY - r.top) / r.height);
    }

    function tast(e) {
        if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();
            avslutt();
        }
    }

    canvasEl.addEventListener('pointerdown', ned, true);
    document.addEventListener('keydown', tast, true);
    canvasEl.classList.add('ks-plasser-port');
    stoppPort = () => {
        canvasEl.removeEventListener('pointerdown', ned, true);
        document.removeEventListener('keydown', tast, true);
        canvasEl.classList.remove('ks-plasser-port');
    };
}

export function stoppPortPlassering() {
    if (stoppPort) {
        const f = stoppPort;
        stoppPort = null;
        f();
    }
}
