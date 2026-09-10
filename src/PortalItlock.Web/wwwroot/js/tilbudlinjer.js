// Native HTML5 dra-og-slipp gjør nettleseren selv om til å nedtone den originale raden mens den
// holdes/dras (kan ikke overstyres med CSS - det er innebygd i nettleserens dra-implementasjon).
// Derfor styres dette manuelt her: musepeker-posisjon spores selv (pointerdown/move/up), raden man
// drar rører ALDRI utseendet sitt, og en egen fullt synlig kloning følger musepekeren i stedet.
window.tilbudlinjer = (function () {
    function kobleManuellDrag(tabellId, dotNetRef) {
        const tabell = document.getElementById(tabellId);
        if (!tabell || tabell.dataset.dragKoblet) {
            return;
        }
        tabell.dataset.dragKoblet = "1";

        tabell.addEventListener('pointerdown', e => {
            if (e.button !== 0) {
                return;
            }
            const handle = e.target.closest('.drag-handle-cell');
            if (!handle) {
                return;
            }
            const rad = handle.closest('tr[data-linje-id]');
            if (!rad) {
                return;
            }

            e.preventDefault();

            const radRect = rad.getBoundingClientRect();
            const ghostTabell = document.createElement('table');
            ghostTabell.className = tabell.className;
            ghostTabell.style.position = 'fixed';
            ghostTabell.style.margin = '0';
            ghostTabell.style.width = radRect.width + 'px';
            ghostTabell.style.pointerEvents = 'none';
            ghostTabell.style.zIndex = '10000';
            ghostTabell.style.boxShadow = '0 8px 24px rgba(0, 0, 0, 0.3)';
            ghostTabell.style.background = getComputedStyle(document.documentElement).getPropertyValue('--itlock-surface') || '#fff';

            const tbody = document.createElement('tbody');
            const kloning = rad.cloneNode(true);
            [...rad.children].forEach((td, i) => {
                kloning.children[i].style.width = td.getBoundingClientRect().width + 'px';
            });
            tbody.appendChild(kloning);
            ghostTabell.appendChild(tbody);
            document.body.appendChild(ghostTabell);

            const offsetX = e.clientX - radRect.left;
            const offsetY = e.clientY - radRect.top;

            const settPosisjon = (clientX, clientY) => {
                ghostTabell.style.left = (clientX - offsetX) + 'px';
                ghostTabell.style.top = (clientY - offsetY) + 'px';
            };
            settPosisjon(e.clientX, e.clientY);

            const finnMaalLinjeId = (clientX, clientY) => {
                const els = document.elementsFromPoint(clientX, clientY);
                const maalEl = els.find(el => el.closest && el.closest(`#${tabellId} tr[data-linje-id]`));
                const maalRad = maalEl ? maalEl.closest('tr[data-linje-id]') : null;
                return maalRad && maalRad !== rad ? parseInt(maalRad.dataset.linjeId, 10) : null;
            };

            const onMove = ev => settPosisjon(ev.clientX, ev.clientY);

            const onUp = ev => {
                document.removeEventListener('pointermove', onMove);
                document.removeEventListener('pointerup', onUp);
                ghostTabell.remove();
                const draggetId = parseInt(rad.dataset.linjeId, 10);
                const maalId = finnMaalLinjeId(ev.clientX, ev.clientY);
                dotNetRef.invokeMethodAsync('FullforDrag', draggetId, maalId);
            };

            document.addEventListener('pointermove', onMove);
            document.addEventListener('pointerup', onUp, { once: true });
        });
    }

    return { kobleManuellDrag };
})();
