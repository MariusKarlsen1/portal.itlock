let currentSaveRef = null;

const dorVisningsmodusKey = 'itlock-dor-visningsmodus';

export function getDorVisningsmodus() {
    try {
        return localStorage.getItem(dorVisningsmodusKey);
    } catch {
        return null;
    }
}

export function setDorVisningsmodus(modus) {
    try {
        localStorage.setItem(dorVisningsmodusKey, modus);
    } catch {
        // Ignorer - f.eks. privat nettlesing der localStorage kan være blokkert.
    }
}

const markorStilKey = 'itlock-dor-markor-stil';

export function getMarkorStil() {
    try {
        return localStorage.getItem(markorStilKey);
    } catch {
        return null;
    }
}

export function setMarkorStil(stil) {
    try {
        localStorage.setItem(markorStilKey, stil);
    } catch {
        // Ignorer - f.eks. privat nettlesing der localStorage kan være blokkert.
    }
}

export function initSaveShortcut(dotNetRef) {
    currentSaveRef = dotNetRef;

    if (!document.__plantegningSaveBound) {
        document.__plantegningSaveBound = true;

        document.addEventListener('keydown', (e) => {
            if ((e.ctrlKey || e.metaKey) && (e.key === 's' || e.key === 'S')) {
                e.preventDefault();
                if (currentSaveRef) {
                    currentSaveRef.invokeMethodAsync('SaveNow');
                }
            }
        });
    }
}

export function getClickPercent(containerEl, clientX, clientY) {
    const rect = containerEl.getBoundingClientRect();
    const x = Math.max(0, Math.min(100, ((clientX - rect.left) / rect.width) * 100));
    const y = Math.max(0, Math.min(100, ((clientY - rect.top) / rect.height) * 100));
    return { x, y };
}

export function initZoomPan(wrapEl, canvasEl, dotNetRef) {
    if (wrapEl.dataset.zoomPanBound === '1') {
        return;
    }
    wrapEl.dataset.zoomPanBound = '1';

    // Som i Radata Probus: tegningen ligger fritt inni rammen. Ved start er
    // hele tegningen synlig og sentrert. Musehjulet zoomer mot markøren (også
    // ut, så tegningen blir mindre enn rammen), venstre museknapp holdt inne +
    // dra flytter tegningen hvor man vil innenfor rammen, og dørprikkene har
    // samme størrelse på skjermen uansett zoom. Rammen fyller resten av
    // vinduet, og en flytende verktøylinje øverst holdes fri ved sentrering.
    const minZoom = 0.2;
    const maxZoom = 16;
    const wheelFactor = 1.15;
    // Litt større steg per trykk på +/- -knappene enn per hakk på musehjulet.
    const buttonFactor = 1.5;
    // Så mye av tegningen må alltid være synlig i rammen, så den ikke kan
    // dras helt bort.
    const minSynlig = 80;

    let zoom = 1;
    let tx = 0;
    let ty = 0;
    let beruort = false;

    const img = canvasEl.querySelector('img');

    // Nettleserens egen «dra»-funksjon (dra et bilde eller markert tekst) skal
    // aldri starte på tegningen. Den ga en sirkel med strek over og stoppet
    // panoreringen til man slapp museknappen.
    if (img) {
        img.draggable = false;
    }
    wrapEl.addEventListener('dragstart', (e) => e.preventDefault());

    // Dørprikkene skal ikke vokse med zoomen - CSS-en ganger størrelsene med
    // --plan-zoom, så den holdes fast på 1.
    canvasEl.style.setProperty('--plan-zoom', 1);
    wrapEl.classList.add('plan-fri');

    function aspect() {
        return img && img.naturalWidth && img.naturalHeight ? img.naturalWidth / img.naturalHeight : 0;
    }

    // Bredden tegningen har ved 100 %: så stor som mulig uten å gå utenfor
    // rammen, verken i bredden eller høyden.
    // Plassen verktøylinjen (og hjelpeteksten nederst) tar over tegningen.
    function innfelt() {
        const ramme = wrapEl.parentElement;
        const bar = ramme ? ramme.querySelector('.plan-toolbar-flytende') : null;
        const hint = ramme ? ramme.querySelector('.plan-hint-flytende') : null;
        return { topp: bar ? bar.offsetHeight + 20 : 0, bunn: hint ? hint.offsetHeight + 12 : 0 };
    }

    // Rammen fyller vinduet fra der den starter og ned til bunnen (ikke på
    // mobil, der CSS-en allerede gir den nesten hele skjermen).
    function tilpassHoyde() {
        if (wrapEl.classList.contains('mobil-plan-canvas-wrap')) {
            return;
        }
        const topp = wrapEl.getBoundingClientRect().top + window.scrollY;
        const h = Math.max(420, Math.floor(window.innerHeight - topp - 16));
        if (wrapEl.style.height !== h + 'px') {
            wrapEl.style.height = h + 'px';
        }
    }

    function baseWidth() {
        const inn = innfelt();
        const w = wrapEl.clientWidth;
        const h = wrapEl.clientHeight - inn.topp - inn.bunn;
        const a = aspect();
        return a && h ? Math.min(w, h * a) : w;
    }

    function bredde() {
        return Math.max(1, baseWidth() * zoom);
    }

    function hoyde() {
        const a = aspect();
        return a ? bredde() / a : canvasEl.offsetHeight;
    }

    function clamp() {
        const W = wrapEl.clientWidth;
        const H = wrapEl.clientHeight;
        const w = bredde();
        const h = hoyde();
        tx = Math.min(W - minSynlig, Math.max(minSynlig - w, tx));
        ty = Math.min(H - minSynlig, Math.max(minSynlig - h, ty));
    }

    function tegn() {
        clamp();
        canvasEl.style.width = bredde() + 'px';
        canvasEl.style.transform = `translate(${tx}px, ${ty}px)`;
    }

    function sentrer() {
        zoom = 1;
        const inn = innfelt();
        tx = (wrapEl.clientWidth - bredde()) / 2;
        ty = inn.topp + (wrapEl.clientHeight - inn.topp - inn.bunn - hoyde()) / 2;
        tegn();
    }

    tilpassHoyde();
    sentrer();
    window.addEventListener('resize', tilpassHoyde);
    if (window.ResizeObserver) {
        // Paneler over rammen (dørvelger, filter, meldinger) flytter toppen.
        new ResizeObserver(() => tilpassHoyde()).observe(document.body);
    }
    if (img && !img.complete) {
        img.addEventListener('load', () => { if (!beruort) { sentrer(); } }, { once: true });
    }
    if (window.ResizeObserver) {
        new ResizeObserver(() => { if (beruort) { tegn(); } else { sentrer(); } }).observe(wrapEl);
    }

    function applyZoom(nextZoom, anchorClientX, anchorClientY) {
        nextZoom = Math.min(maxZoom, Math.max(minZoom, nextZoom));
        if (nextZoom === zoom) {
            return;
        }
        beruort = true;

        // Punktet på tegningen under musepekeren skal bli liggende under den.
        const rect = wrapEl.getBoundingClientRect();
        const ax = anchorClientX - rect.left;
        const ay = anchorClientY - rect.top;
        const ratio = nextZoom / zoom;
        tx = ax - (ax - tx) * ratio;
        ty = ay - (ay - ty) * ratio;
        zoom = nextZoom;
        tegn();
    }

    wrapEl.addEventListener('wheel', (e) => {
        e.preventDefault();
        applyZoom(e.deltaY < 0 ? zoom * wheelFactor : zoom / wheelFactor, e.clientX, e.clientY);
    }, { passive: false });

    // Høyreklikk (trenger ikke holdes) åpner dørvelgeren på stedet - men ikke
    // på en dørprikk eller i et panel.
    wrapEl.addEventListener('contextmenu', (e) => {
        e.preventDefault();
        if (!dotNetRef || e.target.closest('.dor-marker, .utstyr-marker, .dor-velger-panel, .plan-zoom-controls')) {
            return;
        }
        const p = getClickPercent(canvasEl, e.clientX, e.clientY);
        dotNetRef.invokeMethodAsync('OnCanvasRightClicked', p.x, p.y);
    });

    // Dra med venstre museknapp (eller én finger) på tomt område flytter
    // tegningen. Markører håndterer selv venstreknapp-dra (flytting) og
    // stopper den hendelsen før den når hit. Terskelen er romslig fordi et
    // ekte klikk nesten alltid har litt skjelving mellom trykk og slipp.
    const dragThreshold = 8;
    let panning = false;
    let moved = false;
    let startX = 0;
    let startY = 0;
    let startTx = 0;
    let startTy = 0;
    let suppressClick = false;

    // Multi-touch tracking for pinch-zoom. Keyed by pointerId -> {x, y}.
    const activePointers = new Map();
    let pinching = false;
    let pinchStartDist = 0;
    let pinchStartZoom = 1;

    function pinchDistance() {
        const pts = [...activePointers.values()];
        const dx = pts[0].x - pts[1].x;
        const dy = pts[0].y - pts[1].y;
        return Math.sqrt(dx * dx + dy * dy);
    }

    function pinchMidpoint() {
        const pts = [...activePointers.values()];
        return { x: (pts[0].x + pts[1].x) / 2, y: (pts[0].y + pts[1].y) / 2 };
    }

    wrapEl.addEventListener('click', (e) => {
        // While placing something new (utstyr), never eat the click - the user's
        // click must always reach the canvas so it places the item.
        if (canvasEl.dataset.placing === '1') {
            return;
        }
        if (suppressClick) {
            suppressClick = false;
            e.stopPropagation();
            e.preventDefault();
        }
    }, true);

    wrapEl.addEventListener('pointerdown', (e) => {
        // Bare venstre museknapp (og fingre) flytter. Klikk mens man plasserer
        // noe nytt skal alltid nå tegningen, og knapper/felt i paneler inni
        // rammen (dørvelgeren, zoomknappene) skal fungere som vanlig.
        if ((e.pointerType === 'mouse' && e.button !== 0)
            || canvasEl.dataset.placing === '1'
            || e.target.closest('button, a, input, select, textarea, label, .dor-velger-panel, .plan-zoom-controls')) {
            return;
        }

        // Hindrer at trykk + dra markerer tekst på tegningen (som igjen kunne
        // starte nettleserens eget dra-forsøk neste gang man trykket).
        if (e.pointerType === 'mouse') {
            e.preventDefault();
            window.getSelection()?.removeAllRanges();
        }

        activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
        try {
            wrapEl.setPointerCapture(e.pointerId);
        } catch {
            // Kan feile for pekere nettleseren ikke kjenner - dra virker likevel.
        }

        if (activePointers.size === 2) {
            // A second finger just landed - switch from panning to pinching.
            panning = false;
            pinching = true;
            pinchStartDist = pinchDistance();
            pinchStartZoom = zoom;
            return;
        }

        if (activePointers.size === 1) {
            panning = true;
            moved = false;
            startX = e.clientX;
            startY = e.clientY;
            startTx = tx;
            startTy = ty;
        }
    });

    wrapEl.addEventListener('pointermove', (e) => {
        if (!activePointers.has(e.pointerId)) {
            return;
        }
        activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

        if (pinching && activePointers.size >= 2) {
            const dist = pinchDistance();
            if (pinchStartDist > 0) {
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
            beruort = true;
            wrapEl.style.cursor = 'grabbing';
        }
        tx = startTx + dx;
        ty = startTy + dy;
        tegn();
    });

    function endPointer(e) {
        activePointers.delete(e.pointerId);
        try {
            wrapEl.releasePointerCapture(e.pointerId);
        } catch {
            // Already released - ignore.
        }

        if (pinching && activePointers.size < 2) {
            pinching = false;
            // Require a fresh press to resume panning rather than jumping
            // from stale start coordinates.
            panning = false;
            moved = false;
        }

        if (!pinching && panning && activePointers.size === 0) {
            panning = false;
            wrapEl.style.cursor = '';
            if (moved) {
                suppressClick = true;
            }
        }
    }

    wrapEl.addEventListener('pointerup', endPointer);
    wrapEl.addEventListener('pointercancel', endPointer);

    // API for +/- og tilbakestill-knappene - de anker mot midten av rammen.
    function zoomByButton(direction) {
        const rect = wrapEl.getBoundingClientRect();
        const cx = rect.left + rect.width / 2;
        const cy = rect.top + rect.height / 2;
        applyZoom(direction > 0 ? zoom * buttonFactor : zoom / buttonFactor, cx, cy);
    }

    wrapEl.__planZoomApi = {
        zoomIn: () => zoomByButton(1),
        zoomOut: () => zoomByButton(-1),
        reset: () => {
            beruort = false;
            sentrer();
        }
    };
}

export function planZoomIn(wrapEl) {
    wrapEl.__planZoomApi?.zoomIn();
}

export function planZoomOut(wrapEl) {
    wrapEl.__planZoomApi?.zoomOut();
}

export function planResetZoom(wrapEl) {
    wrapEl.__planZoomApi?.reset();
}

export function attachMarkers(containerEl, dotNetRef, locked) {
    containerEl.dataset.locked = locked ? '1' : '0';

    const markers = containerEl.querySelectorAll('.dor-marker[data-dorid]');

    markers.forEach(markerEl => {
        if (markerEl.dataset.dragBound === '1') {
            return;
        }
        markerEl.dataset.dragBound = '1';

        const dorId = parseInt(markerEl.dataset.dorid, 10);
        const dragThreshold = 4;
        let dragging = false;
        let moved = false;
        let startX = 0;
        let startY = 0;

        // Trykk/slipp (pointerdown/pointerup) brukes til å avgjøre klikk vs.
        // dra i BEGGE moduser - en enkel "click"-lytter viste seg upålitelig
        // i låst/normal-visning (trykket nådde ikke alltid frem), så nå
        // brukes samme robuste logikk uansett modus. Selve flyttingen av
        // posisjonen skjer likevel kun når "Rediger posisjoner" er aktiv
        // (containerEl.dataset.locked !== '1') - i låst modus er det bare
        // klikk-deteksjonen som er aktiv, ikke selve flyttingen.
        markerEl.addEventListener('contextmenu', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });

        markerEl.addEventListener('pointerdown', (e) => {
            if (e.button !== 0) {
                return;
            }
            e.preventDefault();
            e.stopPropagation();
            dragging = true;
            moved = false;
            startX = e.clientX;
            startY = e.clientY;
            try { markerEl.setPointerCapture(e.pointerId); } catch { /* ukjent peker */ }
        });

        markerEl.addEventListener('pointermove', (e) => {
            if (!dragging) {
                return;
            }
            if (!moved) {
                const dx = e.clientX - startX;
                const dy = e.clientY - startY;
                if (Math.sqrt(dx * dx + dy * dy) < dragThreshold) {
                    return;
                }
                moved = true;
            }
            if (containerEl.dataset.locked === '1') {
                // Låst/normal-visning: ikke flytt markøren visuelt, bare
                // registrer at det ble en bevegelse (avbryter klikket under).
                return;
            }
            const p = getClickPercent(containerEl, e.clientX, e.clientY);
            markerEl.style.left = p.x + '%';
            markerEl.style.top = p.y + '%';
            markerEl.dataset.x = p.x;
            markerEl.dataset.y = p.y;
        });

        markerEl.addEventListener('pointerup', (e) => {
            if (!dragging) {
                return;
            }
            dragging = false;
            try { markerEl.releasePointerCapture(e.pointerId); } catch { /* allerede sluppet */ }

            if (moved && containerEl.dataset.locked !== '1') {
                const x = parseFloat(markerEl.dataset.x);
                const y = parseFloat(markerEl.dataset.y);
                dotNetRef.invokeMethodAsync('OnDoorMoved', dorId, x, y);
            } else if (!moved) {
                dotNetRef.invokeMethodAsync('OnDoorClicked', dorId);
            }
        });
    });
}

export function setUtstyrPlacing(containerEl, placing) {
    containerEl.dataset.placing = placing ? '1' : '0';
}

export function attachUtstyrMarkers(containerEl, dotNetRef, locked) {
    const markers = containerEl.querySelectorAll('.utstyr-marker[data-utstyrid]');

    markers.forEach(markerEl => {
        if (markerEl.dataset.dragBound === '1') {
            return;
        }
        markerEl.dataset.dragBound = '1';

        const utstyrId = parseInt(markerEl.dataset.utstyrid, 10);
        const dragThreshold = 4;
        let dragging = false;
        let moved = false;
        let startX = 0;
        let startY = 0;

        markerEl.addEventListener('click', (e) => {
            // While placing new utstyr, let the click fall through to the canvas so it
            // places the new item - otherwise clicking near an existing marker silently
            // selects it instead, and the user's "legg til" click appears to do nothing.
            if (containerEl.dataset.placing === '1') {
                return;
            }
            e.stopPropagation();
        });

        markerEl.addEventListener('contextmenu', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });

        markerEl.addEventListener('pointerdown', (e) => {
            if (e.button !== 0) {
                return;
            }
            if (containerEl.dataset.placing === '1') {
                return;
            }
            e.preventDefault();
            e.stopPropagation();
            dragging = true;
            moved = false;
            startX = e.clientX;
            startY = e.clientY;
            try { markerEl.setPointerCapture(e.pointerId); } catch { /* ukjent peker */ }
        });

        markerEl.addEventListener('pointermove', (e) => {
            if (!dragging || containerEl.dataset.locked === '1') {
                return;
            }
            if (!moved) {
                const dx = e.clientX - startX;
                const dy = e.clientY - startY;
                if (Math.sqrt(dx * dx + dy * dy) < dragThreshold) {
                    return;
                }
                moved = true;
            }
            const p = getClickPercent(containerEl, e.clientX, e.clientY);
            markerEl.style.left = p.x + '%';
            markerEl.style.top = p.y + '%';
            markerEl.dataset.x = p.x;
            markerEl.dataset.y = p.y;
            // Kablene følger med mens man drar, ikke først når man slipper.
            flyttKabelEnder(containerEl, utstyrId, p.x, p.y);
        });

        markerEl.addEventListener('pointerup', (e) => {
            if (!dragging) {
                return;
            }
            dragging = false;
            try { markerEl.releasePointerCapture(e.pointerId); } catch { /* allerede sluppet */ }

            if (moved && containerEl.dataset.locked !== '1') {
                const x = parseFloat(markerEl.dataset.x);
                const y = parseFloat(markerEl.dataset.y);
                dotNetRef.invokeMethodAsync('OnUtstyrMoved', utstyrId, x, y);
            } else if (moved) {
                // Låst: et lite dra er verken flytting eller klikk.
            } else {
                dotNetRef.invokeMethodAsync('OnUtstyrClicked', utstyrId);
            }
        });
    });
}

// ---------- Kabeltrekk: myke kurver, knekkpunkter og størrelse på utstyr ----------

// Samme kurve som KabelSti.Kurve i C#: Catmull-Rom gjennom punktene, tegnet som
// kubiske Bézier-kurver. Uten knekkpunkter er det en rett strek.
function kabelKurve(p) {
    if (p.length < 2) {
        return '';
    }
    const f = v => Math.round(v * 1000) / 1000;
    let d = 'M' + f(p[0].x) + ' ' + f(p[0].y);
    if (p.length === 2) {
        return d + ' L' + f(p[1].x) + ' ' + f(p[1].y);
    }
    for (let i = 0; i < p.length - 1; i++) {
        const p0 = p[Math.max(0, i - 1)];
        const p1 = p[i];
        const p2 = p[i + 1];
        const p3 = p[Math.min(p.length - 1, i + 2)];
        const c1x = p1.x + (p2.x - p0.x) / 6, c1y = p1.y + (p2.y - p0.y) / 6;
        const c2x = p2.x - (p3.x - p1.x) / 6, c2y = p2.y - (p3.y - p1.y) / 6;
        d += ' C' + f(c1x) + ' ' + f(c1y) + ' ' + f(c2x) + ' ' + f(c2y) + ' ' + f(p2.x) + ' ' + f(p2.y);
    }
    return d;
}

function lesPunkter(pathEl) {
    return (pathEl.dataset.pts || '').split(';').filter(Boolean).map(s => {
        const xy = s.split(',').map(Number);
        return { x: xy[0], y: xy[1] };
    });
}

function skrivPunkter(pathEl, punkter) {
    pathEl.dataset.pts = punkter.map(p => p.x + ',' + p.y).join(';');
    pathEl.setAttribute('d', kabelKurve(punkter));
}

// Knekkpunktene (uten endepunktene) slik de lagres: "x,y;x,y".
function knekkTekst(punkter) {
    return punkter.slice(1, -1).map(p => (Math.round(p.x * 100) / 100) + ',' + (Math.round(p.y * 100) / 100)).join(';');
}

function kabelSti(containerEl, fid) {
    return containerEl.querySelector('path[data-fid="' + fid + '"]');
}

// Kablene til et utstyr følger med mens det dras.
function flyttKabelEnder(containerEl, utstyrId, x, y) {
    const id = String(utstyrId);
    containerEl.querySelectorAll('path[data-fra="' + id + '"], path[data-til="' + id + '"]').forEach(pathEl => {
        const punkter = lesPunkter(pathEl);
        if (punkter.length < 2) {
            return;
        }
        if (pathEl.dataset.fra === id) {
            punkter[0] = { x, y };
        }
        if (pathEl.dataset.til === id) {
            punkter[punkter.length - 1] = { x, y };
        }
        skrivPunkter(pathEl, punkter);
    });
    // Håndtakene er utdaterte til siden tegnes på nytt etter slipp.
    containerEl.querySelectorAll('.kabel-handtak').forEach(h => { h.style.visibility = 'hidden'; });
}

// Dra «+» midt på en del: lager et nytt knekkpunkt der. Dra et rundt punkt:
// flytter det. Dobbeltklikk et rundt punkt: fjerner det.
export function attachKabelHandtak(containerEl, dotNetRef) {
    containerEl.querySelectorAll('.kabel-handtak').forEach(h => { h.style.visibility = ''; });

    containerEl.querySelectorAll('.kabel-handtak[data-fid]').forEach(handtak => {
        if (handtak.dataset.dragBound === '1') {
            return;
        }
        handtak.dataset.dragBound = '1';

        const fid = handtak.dataset.fid;
        let dragging = false;
        let moved = false;
        let startX = 0;
        let startY = 0;
        let punkter = null;
        let idx = -1;

        handtak.addEventListener('pointerdown', (e) => {
            if (e.button !== 0) {
                return;
            }
            e.preventDefault();
            e.stopPropagation();
            const pathEl = kabelSti(containerEl, fid);
            if (!pathEl) {
                return;
            }
            punkter = lesPunkter(pathEl);
            if (handtak.classList.contains('kabel-midt')) {
                // Nytt punkt settes inn etter starten av delen det ligger på.
                const seg = parseInt(handtak.dataset.seg, 10);
                const a = punkter[seg];
                const b = punkter[seg + 1];
                idx = seg + 1;
                punkter.splice(idx, 0, { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 });
            } else {
                idx = parseInt(handtak.dataset.idx, 10);
            }
            dragging = true;
            moved = false;
            startX = e.clientX;
            startY = e.clientY;
            try { handtak.setPointerCapture(e.pointerId); } catch { /* ukjent peker */ }
        });

        handtak.addEventListener('pointermove', (e) => {
            if (!dragging) {
                return;
            }
            if (!moved && Math.hypot(e.clientX - startX, e.clientY - startY) < 3) {
                return;
            }
            moved = true;
            const p = getClickPercent(containerEl, e.clientX, e.clientY);
            punkter[idx] = { x: p.x, y: p.y };
            handtak.style.left = p.x + '%';
            handtak.style.top = p.y + '%';
            const pathEl = kabelSti(containerEl, fid);
            if (pathEl) {
                skrivPunkter(pathEl, punkter);
            }
        });

        handtak.addEventListener('pointerup', (e) => {
            if (!dragging) {
                return;
            }
            dragging = false;
            try { handtak.releasePointerCapture(e.pointerId); } catch { /* allerede sluppet */ }
            if (moved) {
                dotNetRef.invokeMethodAsync('OnKabelEndret', parseInt(fid, 10), knekkTekst(punkter));
            }
        });

        handtak.addEventListener('click', (e) => e.stopPropagation());

        handtak.addEventListener('dblclick', (e) => {
            e.preventDefault();
            e.stopPropagation();
            if (!handtak.classList.contains('kabel-knekk')) {
                return;
            }
            const pathEl = kabelSti(containerEl, fid);
            if (!pathEl) {
                return;
            }
            const alle = lesPunkter(pathEl);
            alle.splice(parseInt(handtak.dataset.idx, 10), 1);
            skrivPunkter(pathEl, alle);
            dotNetRef.invokeMethodAsync('OnKabelEndret', parseInt(fid, 10), knekkTekst(alle));
        });
    });

    // Størrelseshåndtaket i hjørnet av valgt utstyr.
    containerEl.querySelectorAll('.utstyr-storrelse[data-utstyrid]').forEach(handtak => {
        if (handtak.dataset.dragBound === '1') {
            return;
        }
        handtak.dataset.dragBound = '1';

        const utstyrId = parseInt(handtak.dataset.utstyrid, 10);
        const markerEl = handtak.closest('.utstyr-marker');
        const ikon = markerEl ? markerEl.querySelector('.utstyr-ikon') : null;
        let dragging = false;
        let startX = 0;
        let startY = 0;
        let startW = 0;
        let startH = 0;
        let baseW = 24;
        let baseH = 24;
        let bx = 1;
        let by = 1;

        handtak.addEventListener('pointerdown', (e) => {
            if (e.button !== 0 || !markerEl || !ikon) {
                return;
            }
            e.preventDefault();
            e.stopPropagation();
            const r = ikon.getBoundingClientRect();
            const stil = getComputedStyle(markerEl);
            bx = parseFloat(stil.getPropertyValue('--u-bx')) || 1;
            by = parseFloat(stil.getPropertyValue('--u-by')) || 1;
            startW = r.width;
            startH = r.height;
            baseW = r.width / bx;
            baseH = r.height / by;
            startX = e.clientX;
            startY = e.clientY;
            dragging = true;
            try { handtak.setPointerCapture(e.pointerId); } catch { /* ukjent peker */ }
        });

        handtak.addEventListener('pointermove', (e) => {
            if (!dragging) {
                return;
            }
            // Ikonet er sentrert i markøren, så det vokser like mye begge veier.
            let w = startW + (e.clientX - startX) * 2;
            let h = startH + (e.clientY - startY) * 2;
            if (e.shiftKey) {
                const f = Math.max(w / startW, h / startH);
                w = startW * f;
                h = startH * f;
            }
            bx = Math.min(5, Math.max(0.4, w / baseW));
            by = Math.min(5, Math.max(0.4, h / baseH));
            markerEl.style.setProperty('--u-bx', bx);
            markerEl.style.setProperty('--u-by', by);
        });

        handtak.addEventListener('pointerup', (e) => {
            if (!dragging) {
                return;
            }
            dragging = false;
            try { handtak.releasePointerCapture(e.pointerId); } catch { /* allerede sluppet */ }
            dotNetRef.invokeMethodAsync('OnUtstyrResized', utstyrId, bx, by);
        });

        handtak.addEventListener('click', (e) => e.stopPropagation());
    });
}
