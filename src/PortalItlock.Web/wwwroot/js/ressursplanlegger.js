window.ressursplanlegger = (function () {
    function relativY(elementId, clientY) {
        const el = document.getElementById(elementId);
        if (!el) {
            return 0;
        }

        return clientY - el.getBoundingClientRect().top;
    }

    return { relativY: relativY };
})();
