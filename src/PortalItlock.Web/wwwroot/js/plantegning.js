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
    // ut, så tegningen blir mindre enn rammen), høyre museknapp holdt inne +
    // dra flytter tegningen hvor man vil innenfor rammen, og dørprikkene har
    // samme størrelse på skjermen uansett zoom.
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

    // Dørprikkene skal ikke vokse med zoomen - CSS-en ganger størrelsene med
    // --plan-zoom, så den holdes fast på 1.
    canvasEl.style.setProperty('--plan-zoom', 1);
    wrapEl.classList.add('plan-fri');

    function aspect() {
        return img && img.naturalWidth && img.naturalHeight ? img.naturalWidth / img.naturalHeight : 0;
    }

    // Bredden tegningen har ved 100 %: så stor som mulig uten å gå utenfor
    // rammen, verken i bredden eller høyden.
    function baseWidth() {
        const w = wrapEl.clientWidth;
        const h = wrapEl.clientHeight;
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
        tx = (wrapEl.clientWidth - bredde()) / 2;
        ty = (wrapEl.clientHeight - hoyde()) / 2;
        tegn();
    }

    sentrer();
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

    // Høyre museknapp brukes til å dra tegningen rundt - nettleserens egen
    // høyreklikkmeny skal aldri komme opp over tegningen. Et høyreklikk UTEN
    // å dra åpner dørvelgeren på stedet (se endPointer).
    wrapEl.addEventListener('contextmenu', (e) => {
        e.preventDefault();
    });

    // Dra (høyre eller venstre museknapp, eller én finger) på tomt område
    // flytter tegningen. Markører håndterer selv venstreknapp-dra (flytting)
    // og stopper den hendelsen før den når hit. Terskelen er romslig fordi et
    // ekte klikk nesten alltid har litt skjelving mellom trykk og slipp.
    const dragThreshold = 8;
    let panning = false;
    let panButton = -1;
    let moved = false;
    let startX = 0;
    let startY = 0;
    let startTx = 0;
    let startTy = 0;
    let suppressClick = false;
    let rightTarget = null;

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
        const erHoyre = e.pointerType === 'mouse' && e.button === 2;

        // Venstreklikk mens man plasserer noe nytt skal alltid nå tegningen.
        // Høyre-dra flytter likevel.
        if (canvasEl.dataset.placing === '1' && !erHoyre) {
            return;
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

        if (activePointers.size === 1 && (e.pointerType !== 'mouse' || e.button === 0 || e.button === 2)) {
            if (erHoyre) {
                e.preventDefault();
            }
            panning = true;
            panButton = e.pointerType === 'mouse' ? e.button : 0;
            moved = false;
            rightTarget = erHoyre ? e.target : null;
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
                suppressClick = panButton === 0;
            } else if (panButton === 2 && e.type === 'pointerup' && dotNetRef
                && !(rightTarget && rightTarget.closest && rightTarget.closest('.dor-marker, .utstyr-marker'))) {
                // Høyreklikk uten å dra: åpne dørvelgeren der man klikket.
                const p = getClickPercent(canvasEl, e.clientX, e.clientY);
                dotNetRef.invokeMethodAsync('OnCanvasRightClicked', p.x, p.y);
            }
            panButton = -1;
            rightTarget = null;
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
            markerEl.setPointerCapture(e.pointerId);
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
            markerEl.releasePointerCapture(e.pointerId);

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
            markerEl.setPointerCapture(e.pointerId);
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
        });

        markerEl.addEventListener('pointerup', (e) => {
            if (!dragging) {
                return;
            }
            dragging = false;
            markerEl.releasePointerCapture(e.pointerId);

            if (moved) {
                const x = parseFloat(markerEl.dataset.x);
                const y = parseFloat(markerEl.dataset.y);
                dotNetRef.invokeMethodAsync('OnUtstyrMoved', utstyrId, x, y);
            } else {
                dotNetRef.invokeMethodAsync('OnUtstyrClicked', utstyrId);
            }
        });
    });
}
