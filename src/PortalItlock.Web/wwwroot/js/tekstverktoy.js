window.tekstverktoy = (function () {
    const lastSelection = {};

    function trackSelection(id) {
        const el = document.getElementById(id);
        if (!el) {
            return;
        }

        const update = () => {
            lastSelection[id] = { start: el.selectionStart, end: el.selectionEnd };
        };

        el.addEventListener('keyup', update);
        el.addEventListener('mouseup', update);
        el.addEventListener('select', update);
        update();
    }

    function getSelection(id) {
        const el = document.getElementById(id);
        const sel = lastSelection[id] || { start: el.value.length, end: el.value.length };
        return { el, start: sel.start, end: sel.end };
    }

    function wrapSelection(id, before, after) {
        const { el, start, end } = getSelection(id);
        const value = el.value;
        const selected = value.substring(start, end);
        const newValue = value.substring(0, start) + before + selected + after + value.substring(end);
        el.value = newValue;
        const newStart = start + before.length;
        const newEnd = newStart + selected.length;
        el.focus();
        el.setSelectionRange(newStart, newEnd);
        lastSelection[id] = { start: newStart, end: newEnd };
        return newValue;
    }

    function uppercaseSelection(id) {
        const { el, start, end } = getSelection(id);
        const value = el.value;
        const selected = value.substring(start, end).toUpperCase();
        const newValue = value.substring(0, start) + selected + value.substring(end);
        el.value = newValue;
        el.focus();
        el.setSelectionRange(start, start + selected.length);
        lastSelection[id] = { start, end: start + selected.length };
        return newValue;
    }

    function insertAtLineStart(id, prefix) {
        const { el, start } = getSelection(id);
        const value = el.value;
        const lineStart = value.lastIndexOf('\n', start - 1) + 1;
        const newValue = value.substring(0, lineStart) + prefix + value.substring(lineStart);
        el.value = newValue;
        const newPos = start + prefix.length;
        el.focus();
        el.setSelectionRange(newPos, newPos);
        lastSelection[id] = { start: newPos, end: newPos };
        return newValue;
    }

    return { trackSelection, wrapSelection, uppercaseSelection, insertAtLineStart };
})();
