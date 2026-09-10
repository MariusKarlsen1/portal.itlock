window.aiassistent = (function () {
    function scrollTilBunn(elementId) {
        const el = document.getElementById(elementId);
        if (el) {
            el.scrollTop = el.scrollHeight;
        }
    }

    return { scrollTilBunn };
})();
