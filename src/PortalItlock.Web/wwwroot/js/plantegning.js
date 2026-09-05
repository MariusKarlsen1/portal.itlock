let currentSaveRef = null;

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
    const maxZoom = 3;
    const step = 0.12;
    let zoom = 1;

    canvasEl.style.width = '100%';
    canvasEl.style.setProperty('--plan-zoom', zoom);

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
        canvasEl.style.setProperty('--plan-zoom', zoom);

        wrapEl.scrollLeft = (wrapEl.scrollLeft + cursorX) * ratio - cursorX;
        wrapEl.scrollTop = (wrapEl.scrollTop + cursorY) * ratio - cursorY;
    }, { passive: false });

    // Right-click (no need to hold) opens the door picker at that spot.
    wrapEl.addEventListener('contextmenu', (e) => {
        e.preventDefault();
        const p = getClickPercent(canvasEl, e.clientX, e.clientY);
        dotNetRef.invokeMethodAsync('OnCanvasRightClicked', p.x, p.y);
    });

    // Left-button hold + drag on empty canvas pans the drawing. Markers
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
        if (e.button !== 0) {
            return;
        }
        // Don't engage pan-detection at all while placing - there is nothing to
        // pan-vs-click disambiguate here, and doing so only risks losing the click.
        if (canvasEl.dataset.placing === '1') {
            return;
        }
        panning = true;
        moved = false;
        startX = e.clientX;
        startY = e.clientY;
        startScrollLeft = wrapEl.scrollLeft;
        startScrollTop = wrapEl.scrollTop;
        wrapEl.setPointerCapture(e.pointerId);
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
        wrapEl.releasePointerCapture(e.pointerId);
        wrapEl.style.cursor = '';

        if (moved) {
            suppressClick = true;
        }
    });
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
