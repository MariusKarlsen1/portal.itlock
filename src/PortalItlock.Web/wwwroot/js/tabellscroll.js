window.tabellscroll = (function () {
    function koble(topId, wrapId) {
        const top = document.getElementById(topId);
        const wrap = document.getElementById(wrapId);
        if (!top || !wrap) {
            return;
        }

        const table = wrap.querySelector('table');
        const inner = top.querySelector('.table-scroll-top-inner');
        if (!table || !inner) {
            return;
        }

        function oppdaterBredde() {
            inner.style.width = table.scrollWidth + 'px';
        }
        oppdaterBredde();

        if (typeof ResizeObserver !== 'undefined') {
            new ResizeObserver(oppdaterBredde).observe(table);
        } else {
            window.addEventListener('resize', oppdaterBredde);
        }

        let synkroniserer = false;
        top.addEventListener('scroll', function () {
            if (synkroniserer) {
                return;
            }
            synkroniserer = true;
            wrap.scrollLeft = top.scrollLeft;
            synkroniserer = false;
        });
        wrap.addEventListener('scroll', function () {
            if (synkroniserer) {
                return;
            }
            synkroniserer = true;
            top.scrollLeft = wrap.scrollLeft;
            synkroniserer = false;
        });
    }

    return { koble: koble };
})();
