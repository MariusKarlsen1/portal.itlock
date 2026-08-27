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

    wrapEl.addEventListener('wheel', (e) => {
        e.preventDefault();
        const prevZoom = zoom;
        const next = zoom + (e.deltaY < 0 ? step : -step);
        zoom = Math.min(maxZoom, Math.max(minZoom, next));
        if (zoom === prevZoom) {
            return;
        }

        const rect = wrapEl.getBoundingClientRect();
        const cursorX = e.clientX - rect.left;
        const cursorY = e.clientY - rect.top;
        const ratio = zoom / prevZoom;

        canvasEl.style.width = (zoom * 100) + '%';

        wrapEl.scrollLeft = (wrapEl.scrollLeft + cursorX) * ratio - cursorX;
        wrapEl.scrollTop = (wrapEl.scrollTop + cursorY) * ratio - cursorY;
    }, { passive: false });

    const dragThreshold = 4;
    let panning = false;
    let moved = false;
    let startX = 0;
    let startY = 0;
    let startScrollLeft = 0;
    let startScrollTop = 0;
    let suppressClick = false;

    wrapEl.addEventListener('click', (e) => {
        if (suppressClick) {
            suppressClick = false;
            e.stopPropagation();
            e.preventDefault();
        }
    }, true);

    wrapEl.addEventListener('pointerdown', (e) => {
        if (e.button !== 0 || canvasEl.dataset.locked === '0') {
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

    wrapEl.addEventListener('pointerup', (e) => {
        if (!panning) {
            return;
        }
        panning = false;
        safeRelease(wrapEl, e.pointerId);
        wrapEl.style.cursor = '';

        if (moved) {
            suppressClick = true;
        }
    });
}

let currentStrekTegning = null;

const SNAP_RADIUS_PX = 14;

function findPunktNaer(canvasEl, clientX, clientY) {
    const punkter = canvasEl.querySelectorAll('.kobling-symbol-marker[data-elementtype="Punkt"]');
    const rect = canvasEl.getBoundingClientRect();
    let best = null;
    let bestDist = SNAP_RADIUS_PX;

    punkter.forEach(m => {
        const px = rect.left + (parseFloat(m.dataset.x) / 100) * rect.width;
        const py = rect.top + (parseFloat(m.dataset.y) / 100) * rect.height;
        const dist = Math.hypot(clientX - px, clientY - py);
        if (dist <= bestDist) {
            bestDist = dist;
            best = { x: parseFloat(m.dataset.x), y: parseFloat(m.dataset.y) };
        }
    });

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

const STREK_HIT_PX = 8;

function finnStrekNaer(canvasEl, clientX, clientY) {
    const rect = canvasEl.getBoundingClientRect();
    const polylines = canvasEl.querySelectorAll('.kobling-strek-svg-overlay polyline[data-strekid]');
    let best = null;
    let bestDist = STREK_HIT_PX;

    polylines.forEach(pl => {
        const raw = pl.getAttribute('points') || '';
        const pts = raw.trim().split(/\s+/).filter(Boolean).map(par => {
            const [px, py] = par.split(',').map(Number);
            return {
                x: rect.left + (px / 100) * rect.width,
                y: rect.top + (py / 100) * rect.height
            };
        });
        for (let i = 0; i < pts.length - 1; i++) {
            const dist = avstandTilLinjestykke({ x: clientX, y: clientY }, pts[i], pts[i + 1]);
            if (dist <= bestDist) {
                bestDist = dist;
                best = parseInt(pl.dataset.strekid, 10);
            }
        }
    });

    return best;
}

const MIN_TURN_PX = 10;

// Tegn strek = trykk ned, dra i den retningen streken skal gå, slipp.
// Snur du retning underveis (uten å slippe museknappen) settes det automatisk et
// hjørne der du snur, slik at hele streken blir én sammenhengende, vinkelrett linje.
export function startStrekTegning(canvasEl, dotNetRef, farge, tykkelse) {
    stopStrekTegning();

    const svgOverlay = canvasEl.querySelector('.kobling-strek-svg-overlay');
    let previewLine = null;
    let dragging = false;
    let mode = null; // 'draw' | 'move'
    let vertices = [];
    let currentAxis = null;
    let moveStrekId = null;
    let moveOrigPunkter = null;
    let moveStartRaw = null;
    let moveMoved = false;

    function ensurePreview() {
        if (!previewLine) {
            previewLine = document.createElementNS('http://www.w3.org/2000/svg', 'polyline');
            previewLine.setAttribute('fill', 'none');
            previewLine.setAttribute('stroke', farge);
            previewLine.setAttribute('stroke-width', tykkelse);
            previewLine.setAttribute('vector-effect', 'non-scaling-stroke');
            previewLine.setAttribute('stroke-dasharray', '3,2');
            previewLine.style.pointerEvents = 'none';
            svgOverlay.appendChild(previewLine);
        }
        return previewLine;
    }

    function pointsToStr(pts) {
        return pts.map(p => `${p.x},${p.y}`).join(' ');
    }

    function parsePointsAttr(str) {
        return (str || '').trim().split(/\s+/).filter(Boolean).map(par => {
            const [x, y] = par.split(',').map(Number);
            return { x, y };
        });
    }

    function fjernPreview() {
        if (previewLine) {
            previewLine.remove();
            previewLine = null;
        }
    }

    function tentativeEnd(raw) {
        const last = vertices[vertices.length - 1];
        if (currentAxis === 'y') {
            return { x: last.x, y: raw.y };
        }
        if (currentAxis === 'x') {
            return { x: raw.x, y: last.y };
        }
        return last;
    }

    function onPointerDown(e) {
        if (e.button !== 0) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();

        const strekId = finnStrekNaer(canvasEl, e.clientX, e.clientY);
        if (strekId !== null) {
            const polyEl = canvasEl.querySelector(`.kobling-strek-svg-overlay polyline[data-strekid="${strekId}"]`);
            if (polyEl) {
                mode = 'move';
                moveStrekId = strekId;
                moveOrigPunkter = parsePointsAttr(polyEl.getAttribute('points'));
                moveStartRaw = getClickPercent(canvasEl, e.clientX, e.clientY);
                moveMoved = false;
                dragging = true;
                safeCapture(canvasEl, e.pointerId);
                return;
            }
        }

        mode = 'draw';
        const snap = findPunktNaer(canvasEl, e.clientX, e.clientY);
        vertices = [snap ?? getClickPercent(canvasEl, e.clientX, e.clientY)];
        currentAxis = null;
        dragging = true;
        safeCapture(canvasEl, e.pointerId);
    }

    function onPointerMove(e) {
        if (!dragging) {
            return;
        }

        if (mode === 'move') {
            const raw = getClickPercent(canvasEl, e.clientX, e.clientY);
            const dx = raw.x - moveStartRaw.x;
            const dy = raw.y - moveStartRaw.y;
            const rectPx = canvasEl.getBoundingClientRect();
            if (!moveMoved) {
                const movedPx = Math.hypot((dx / 100) * rectPx.width, (dy / 100) * rectPx.height);
                if (movedPx > 4) {
                    moveMoved = true;
                }
            }
            const polyEl = canvasEl.querySelector(`.kobling-strek-svg-overlay polyline[data-strekid="${moveStrekId}"]`);
            if (polyEl) {
                polyEl.setAttribute('points', pointsToStr(moveOrigPunkter.map(p => ({ x: p.x + dx, y: p.y + dy }))));
            }
            return;
        }

        const snap = findPunktNaer(canvasEl, e.clientX, e.clientY);
        const raw = snap ?? getClickPercent(canvasEl, e.clientX, e.clientY);
        const last = vertices[vertices.length - 1];
        const dx = raw.x - last.x;
        const dy = raw.y - last.y;
        const rectPx = canvasEl.getBoundingClientRect();

        if (currentAxis === null) {
            const movedPx = Math.hypot((dx / 100) * rectPx.width, (dy / 100) * rectPx.height);
            if (movedPx > 4) {
                currentAxis = Math.abs(dx) >= Math.abs(dy) ? 'x' : 'y';
            }
        } else {
            const annenDeltaPct = currentAxis === 'x' ? dy : dx;
            const annenPx = currentAxis === 'x'
                ? (Math.abs(annenDeltaPct) / 100) * rectPx.height
                : (Math.abs(annenDeltaPct) / 100) * rectPx.width;
            if (annenPx > MIN_TURN_PX) {
                vertices.push(tentativeEnd(raw));
                currentAxis = currentAxis === 'x' ? 'y' : 'x';
            }
        }

        ensurePreview().setAttribute('points', pointsToStr([...vertices, tentativeEnd(raw)]));
    }

    function onPointerUp(e) {
        if (!dragging) {
            return;
        }
        dragging = false;
        safeRelease(canvasEl, e.pointerId);

        if (mode === 'move') {
            const strekId = moveStrekId;
            const origPunkter = moveOrigPunkter;
            const flyttet = moveMoved;
            const raw = getClickPercent(canvasEl, e.clientX, e.clientY);
            const dx = raw.x - moveStartRaw.x;
            const dy = raw.y - moveStartRaw.y;
            mode = null;
            moveStrekId = null;
            moveOrigPunkter = null;

            if (!flyttet) {
                // Klikk uten drag - slett streken.
                dotNetRef.invokeMethodAsync('OnStrekKlikkSlett', strekId);
                return;
            }

            const nyePunkter = origPunkter.map(p => ({ X: p.x + dx, Y: p.y + dy }));
            dotNetRef.invokeMethodAsync('OnStrekFlyttet', strekId, JSON.stringify(nyePunkter));
            return;
        }

        fjernPreview();

        if (currentAxis === null) {
            vertices = [];
            return;
        }

        const snap = findPunktNaer(canvasEl, e.clientX, e.clientY);
        const raw = snap ?? getClickPercent(canvasEl, e.clientX, e.clientY);
        const last = vertices[vertices.length - 1];
        const slutt = tentativeEnd(raw);
        const dist = Math.hypot(slutt.x - last.x, slutt.y - last.y);
        const alle = dist < 1 ? vertices : [...vertices, slutt];

        vertices = [];
        currentAxis = null;

        if (alle.length < 2) {
            return;
        }

        const result = JSON.stringify(alle.map(p => ({ X: p.x, Y: p.y })));
        dotNetRef.invokeMethodAsync('OnStrekFerdig', result);
    }

    function onKeyDown(e) {
        if (e.key === 'Escape' && dragging) {
            dragging = false;
            if (mode === 'move') {
                const polyEl = canvasEl.querySelector(`.kobling-strek-svg-overlay polyline[data-strekid="${moveStrekId}"]`);
                if (polyEl && moveOrigPunkter) {
                    polyEl.setAttribute('points', pointsToStr(moveOrigPunkter));
                }
            } else {
                fjernPreview();
            }
            mode = null;
            moveStrekId = null;
            moveOrigPunkter = null;
            vertices = [];
            currentAxis = null;
        }
    }

    function cleanup() {
        canvasEl.removeEventListener('pointerdown', onPointerDown);
        canvasEl.removeEventListener('pointermove', onPointerMove);
        canvasEl.removeEventListener('pointerup', onPointerUp);
        document.removeEventListener('keydown', onKeyDown);
        fjernPreview();
        if (currentStrekTegning === api) {
            currentStrekTegning = null;
        }
    }

    canvasEl.addEventListener('pointerdown', onPointerDown);
    canvasEl.addEventListener('pointermove', onPointerMove);
    canvasEl.addEventListener('pointerup', onPointerUp);
    document.addEventListener('keydown', onKeyDown);

    const api = { cleanup };
    currentStrekTegning = api;
}

export function stopStrekTegning() {
    if (currentStrekTegning) {
        currentStrekTegning.cleanup();
        currentStrekTegning = null;
    }
}

const GRID_STEP = 2;

function snapToGrid(value) {
    return Math.round(value / GRID_STEP) * GRID_STEP;
}

export function attachSymbolMarkers(containerEl, dotNetRef, locked) {
    containerEl.dataset.locked = locked ? '1' : '0';

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
                            startTop: parseFloat(el.style.top) || 0
                        }));
                } else {
                    groupMarkers = [];
                }

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
                }
                const rect = containerEl.getBoundingClientRect();
                const pdx = (dx / rect.width) * 100;
                const pdy = (dy / rect.height) * 100;

                groupMarkers.forEach(g => {
                    const nx = snapToGrid(Math.max(0, g.startLeft + pdx));
                    const ny = snapToGrid(Math.max(0, g.startTop + pdy));
                    g.el.style.left = nx + '%';
                    g.el.style.top = ny + '%';
                    const w = parseFloat(g.el.style.width) || 0;
                    const h = parseFloat(g.el.style.height) || 0;
                    g.el.dataset.x = nx + w / 2;
                    g.el.dataset.y = ny + h / 2;
                });
            });

            markerEl.addEventListener('pointerup', (e) => {
                if (!dragging) {
                    return;
                }
                dragging = false;
                safeRelease(markerEl, e.pointerId);

                if (moved) {
                    const payload = groupMarkers.map(g => ({
                        Id: parseInt(g.el.dataset.symbolid, 10),
                        X: parseFloat(g.el.style.left),
                        Y: parseFloat(g.el.style.top)
                    }));
                    if (payload.length === 1) {
                        dotNetRef.invokeMethodAsync('OnSymbolMoved', payload[0].Id, payload[0].X, payload[0].Y);
                    } else {
                        dotNetRef.invokeMethodAsync('OnSymbolsMoved', JSON.stringify(payload));
                    }
                } else {
                    dotNetRef.invokeMethodAsync('OnSymbolClicked', symbolId, e.shiftKey);
                }
                groupMarkers = [];
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
                safeCapture(handleEl, e.pointerId);
            });

            handleEl.addEventListener('pointermove', (e) => {
                if (!resizing) {
                    return;
                }
                const rect = containerEl.getBoundingClientRect();
                const dw = ((e.clientX - startClientX) / rect.width) * 100;
                const dh = ((e.clientY - startClientY) / rect.height) * 100;
                const newWidth = snapToGrid(Math.max(2, startWidthPct + dw));
                const newHeight = snapToGrid(Math.max(2, startHeightPct + dh));
                markerEl.style.width = newWidth + '%';
                markerEl.style.height = newHeight + '%';
                markerEl.dataset.w = newWidth;
                markerEl.dataset.h = newHeight;
                const left = parseFloat(markerEl.style.left) || 0;
                const top = parseFloat(markerEl.style.top) || 0;
                markerEl.dataset.x = left + newWidth / 2;
                markerEl.dataset.y = top + newHeight / 2;
            });

            handleEl.addEventListener('pointerup', (e) => {
                if (!resizing) {
                    return;
                }
                resizing = false;
                safeRelease(handleEl, e.pointerId);
                const w = parseFloat(markerEl.dataset.w) || startWidthPct;
                const h = parseFloat(markerEl.dataset.h) || startHeightPct;
                dotNetRef.invokeMethodAsync('OnSymbolResized', symbolId, w, h);
            });
        }
    });
}

const NUDGE_STEP = 1;
let nudgeSaveTimer = null;

export function enableKeyboardShortcuts(containerEl, dotNetRef) {
    if (containerEl.dataset.keysBound === '1') {
        return;
    }
    containerEl.dataset.keysBound = '1';

    document.addEventListener('keydown', (e) => {
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
    });
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
