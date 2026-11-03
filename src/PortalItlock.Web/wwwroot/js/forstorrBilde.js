// Forstørret varebilde når man holder musen over et lite bilde med
// [data-forstorr="url"] (beslag i dørkortet), som i søket på tilbud.
// Forstørrelsen ligger fast på skjermen ved musepekeren, så den ikke klippes
// av tabeller med rullefelt. Vises bare når det lille bildet faktisk finnes.
(function () {
    var vis = null;

    function lag() {
        var img = document.createElement('img');
        img.className = 'forstorr-bilde';
        img.alt = '';
        document.body.appendChild(img);
        return img;
    }

    function plasser(img, e) {
        var b = img.offsetWidth || 260, h = img.offsetHeight || 260, m = 16;
        var x = e.clientX + m, y = e.clientY + m;
        if (x + b > window.innerWidth - 8) x = e.clientX - b - m;
        if (y + h > window.innerHeight - 8) y = window.innerHeight - h - 8;
        img.style.left = Math.max(8, x) + 'px';
        img.style.top = Math.max(8, y) + 'px';
    }

    document.addEventListener('mouseover', function (e) {
        var el = e.target.closest && e.target.closest('[data-forstorr]');
        if (!el) return;
        var liten = el.querySelector('img');
        if (!liten || liten.style.display === 'none' || !liten.complete || !liten.naturalWidth) return;
        vis = vis || lag();
        if (vis.getAttribute('src') !== el.dataset.forstorr) vis.src = el.dataset.forstorr;
        vis.classList.add('pa');
        plasser(vis, e);
    });
    document.addEventListener('mousemove', function (e) {
        if (vis && vis.classList.contains('pa')) plasser(vis, e);
    });
    document.addEventListener('mouseout', function (e) {
        var el = e.target.closest && e.target.closest('[data-forstorr]');
        if (el && vis && !el.contains(e.relatedTarget)) vis.classList.remove('pa');
    });
})();
