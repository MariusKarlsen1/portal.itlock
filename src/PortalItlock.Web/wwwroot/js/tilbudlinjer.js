// Nettleserens standard "spøkelsesbilde" ved dra-og-slipp av en tabellrad er halvgjennomsiktig
// og mister kolonnebreddene (rader har ingen egen bredde utenfor <table>-konteksten), så den ser
// svak/ødelagt ut. Denne lager i stedet en fullt synlig kloning av raden (med riktige kolonnebredder
// kopiert inn) og bruker den som eget dra-bilde via setDragImage, slik at raden man drar tydelig
// følger musepekeren i original styrke helt til man slipper.
window.tilbudlinjer = (function () {
    function kobleDragGhost(tabellId) {
        const tabell = document.getElementById(tabellId);
        if (!tabell || tabell.dataset.dragGhostKoblet) {
            return;
        }
        tabell.dataset.dragGhostKoblet = "1";

        tabell.addEventListener('dragstart', e => {
            const rad = e.target.closest('tr[draggable="true"]');
            if (!rad) {
                return;
            }

            const radRect = rad.getBoundingClientRect();
            const ghostTabell = document.createElement('table');
            ghostTabell.className = tabell.className;
            ghostTabell.style.position = 'fixed';
            ghostTabell.style.top = '-2000px';
            ghostTabell.style.left = '0';
            ghostTabell.style.width = radRect.width + 'px';
            ghostTabell.style.margin = '0';
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

            e.dataTransfer.setDragImage(ghostTabell, e.clientX - radRect.left, e.clientY - radRect.top);
            e.dataTransfer.effectAllowed = 'move';

            setTimeout(() => ghostTabell.remove(), 0);
        });
    }

    return { kobleDragGhost };
})();
