export function getClickPercent(containerEl, clientX, clientY) {
    const rect = containerEl.getBoundingClientRect();
    const x = Math.max(0, Math.min(100, ((clientX - rect.left) / rect.width) * 100));
    const y = Math.max(0, Math.min(100, ((clientY - rect.top) / rect.height) * 100));
    return { x, y };
}

export function attachMarkers(containerEl, dotNetRef) {
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

        markerEl.addEventListener('pointerdown', (e) => {
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
