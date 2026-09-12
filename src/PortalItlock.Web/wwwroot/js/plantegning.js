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

    const minZoom = 0.4;
    // Ingen reell øvre grense i praksis - montører må kunne zoome helt inn på
    // små tekstetiketter/hotspots på tegningen.
    const maxZoom = 12;
    // Pinch-zoom med fingre skal bare kunne zoome INN, ikke ut forbi normal
    // visning - man skal ikke kunne klype tegningen mindre enn 100%.
    const minPinchZoom = 1;
    const step = 0.12;
    // Litt større steg per trykk på +/- -knappene enn per hakk på musehjulet,
    // slik at et enkelt trykk faktisk oppleves som en synlig endring.
    const buttonStep = 0.4;
    let zoom = 1;

    canvasEl.style.width = '100%';
    canvasEl.style.setProperty('--plan-zoom', zoom);

    function applyZoom(nextZoom, anchorClientX, anchorClientY) {
        const prevZoom = zoom;
        zoom = nextZoom;
        if (zoom === prevZoom) {
            return;
        }

        const rect = wrapEl.getBoundingClientRect();
        const anchorX = anchorClientX - rect.left;
        const anchorY = anchorClientY - rect.top;
        const ratio = zoom / prevZoom;

        canvasEl.style.width = (zoom * 100) + '%';
        canvasEl.style.setProperty('--plan-zoom', zoom);

        wrapEl.scrollLeft = (wrapEl.scrollLeft + anchorX) * ratio - anchorX;
        wrapEl.scrollTop = (wrapEl.scrollTop + anchorY) * ratio - anchorY;
    }

    wrapEl.addEventListener('wheel', (e) => {
        e.preventDefault();
        const next = Math.min(maxZoom, Math.max(minZoom, zoom + (e.deltaY < 0 ? step : -step)));
        applyZoom(next, e.clientX, e.clientY);
    }, { passive: false });

    // Right-click (no need to hold) opens the door picker at that spot.
    wrapEl.addEventListener('contextmenu', (e) => {
        e.preventDefault();
        const p = getClickPercent(canvasEl, e.clientX, e.clientY);
        dotNetRef.invokeMethodAsync('OnCanvasRightClicked', p.x, p.y);
    });

    // Left-button hold + drag on empty canvas pans the drawing (mouse or
    // single-finger touch). A second finger touching down switches to
    // pinch-zoom instead - see the activePointers tracking below. Markers
    // handle their own left-button drag (to reposition) and stop the
    // event from reaching here, so this only fires for background drags.
    // Threshold is generous because a real click (e.g. placing new utstyr/dør)
    // almost always has a few px of hand jitter between press and release -
    // too low a threshold misclassifies that as a pan and silently eats the click.
    const dragThreshold = 12;
    let panning = false;
    let moved = false;
    let startX = 0;
    let startY = 0;
    let startScrollLeft = 0;
    let startScrollTop = 0;
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
        // Don't engage pan/pinch-detection at all while placing - there is
        // nothing to pan-vs-click disambiguate here, and doing so only
        // risks losing the click.
        if (canvasEl.dataset.placing === '1') {
            return;
        }

        activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
        wrapEl.setPointerCapture(e.pointerId);

        if (activePointers.size === 2) {
            // A second finger just landed - switch from panning to pinching.
            panning = false;
            pinching = true;
            pinchStartDist = pinchDistance();
            pinchStartZoom = zoom;
            return;
        }

        // Single-finger touch is left alone entirely so the browser's native
        // scrolling takes over (and chains to the page once the drawing can't
        // scroll further) - only mouse drag uses the custom pan logic below.
        if (activePointers.size === 1 && e.button === 0 && e.pointerType === 'mouse') {
            panning = true;
            moved = false;
            startX = e.clientX;
            startY = e.clientY;
            startScrollLeft = wrapEl.scrollLeft;
            startScrollTop = wrapEl.scrollTop;
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
                const next = Math.min(maxZoom, Math.max(minPinchZoom, pinchStartZoom * (dist / pinchStartDist)));
                const mid = pinchMidpoint();
                applyZoom(next, mid.x, mid.y);
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

    // API for de flytende +/- og tilbakestill-knappene på mobil (se
    // planZoomIn/planZoomOut/planResetZoom under) - knappene har ingen
    // musepeker-posisjon å zoome mot, så de anker mot midten av det synlige
    // utsnittet i stedet.
    function zoomByButton(direction) {
        const rect = wrapEl.getBoundingClientRect();
        const cx = rect.left + rect.width / 2;
        const cy = rect.top + rect.height / 2;
        const next = Math.min(maxZoom, Math.max(minZoom, zoom + direction * buttonStep));
        applyZoom(next, cx, cy);
    }

    wrapEl.__planZoomApi = {
        zoomIn: () => zoomByButton(1),
        zoomOut: () => zoomByButton(-1),
        reset: () => {
            const rect = wrapEl.getBoundingClientRect();
            applyZoom(1, rect.left, rect.top);
            wrapEl.scrollLeft = 0;
            wrapEl.scrollTop = 0;
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

        // Låst/mobil-visning: åpne døren kun ved et ekte trykk direkte på
        // selve sirkelen (ikke hele markøren med etikett+funksjoner rundt) -
        // en native "click" respekterer automatisk nettleserens egne regler
        // for om dette faktisk var et trykk eller en finger som beveget seg
        // (f.eks. som del av en klype-zoom som tilfeldigvis startet der).
        const dot = markerEl.querySelector('.dor-marker-dot') || markerEl;
        dot.addEventListener('click', (e) => {
            if (containerEl.dataset.locked !== '1') {
                return;
            }
            e.stopPropagation();
            dotNetRef.invokeMethodAsync('OnDoorClicked', dorId);
        });

        markerEl.addEventListener('click', (e) => {
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
            if (containerEl.dataset.locked === '1') {
                // Ikke fang pekeren her i låst/mobil-visning - la den boble
                // videre til klype/dra-håndteringen på selve
                // tegningselementet. Ellers mister en klype-zoom det ene
                // fingertrykket (og brytes) hvis det lander på en
                // dørmarkør, og enda verre: siden dørene er låst her og
                // dermed ikke kan telle som "flyttet", ble ethvert slikt
                // trykk feilaktig tolket som et klikk som åpnet døren.
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
                dotNetRef.invokeMethodAsync('OnDoorMoved', dorId, x, y);
            } else {
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
