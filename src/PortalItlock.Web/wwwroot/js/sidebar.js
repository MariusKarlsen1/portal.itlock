window.sidebarMeny = (function () {
    const storageKey = 'itlock-sidebar';

    function isCollapsed() {
        return document.documentElement.getAttribute('data-sidebar') === 'collapsed';
    }

    function apply() {
        if (localStorage.getItem(storageKey) === 'collapsed') {
            document.documentElement.setAttribute('data-sidebar', 'collapsed');
        }
    }

    function toggle() {
        const next = isCollapsed() ? 'expanded' : 'collapsed';
        document.documentElement.setAttribute('data-sidebar', next);
        localStorage.setItem(storageKey, next);
        return next === 'collapsed';
    }

    return { isCollapsed, apply, toggle };
})();
