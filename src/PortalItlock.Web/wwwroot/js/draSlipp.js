// Dra og slipp i lister (plantegninger i byggetrinn): markerer elementet som dras
// ([data-dra]) og målet man holder over ([data-slipp]) med klassene .drar og
// .slipp-mal. Selve flyttingen gjøres av Blazor (@ondragstart / @ondrop), så dette
// er bare det visuelle - uten et kall til serveren for hver musebevegelse.
(function () {
    function rydd() {
        document.querySelectorAll('.drar, .slipp-mal').forEach(function (el) {
            el.classList.remove('drar', 'slipp-mal');
        });
    }

    document.addEventListener('dragstart', function (e) {
        var el = e.target.closest && e.target.closest('[data-dra]');
        if (!el || el.getAttribute('draggable') !== 'true') return;
        // Firefox starter ikke dra uten data.
        if (e.dataTransfer) {
            e.dataTransfer.setData('text/plain', el.dataset.dra);
            e.dataTransfer.effectAllowed = 'move';
        }
        var kort = el.dataset.dra === 'gruppe' ? el.closest('[data-slipp]') : el;
        if (kort) setTimeout(function () { kort.classList.add('drar'); }, 0);
    });

    document.addEventListener('dragover', function (e) {
        var mal = e.target.closest && e.target.closest('[data-slipp]');
        document.querySelectorAll('.slipp-mal').forEach(function (el) {
            if (el !== mal) el.classList.remove('slipp-mal');
        });
        if (mal && !mal.classList.contains('drar')) mal.classList.add('slipp-mal');
    });

    document.addEventListener('drop', rydd);
    document.addEventListener('dragend', rydd);
})();
