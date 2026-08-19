window.richtext = (function () {
    const editors = {};

    function init(id, initialHtml, dotnetRef) {
        const el = document.getElementById(id);
        if (!el) {
            return;
        }

        el.innerHTML = initialHtml || '';
        editors[id] = dotnetRef;

        document.execCommand('defaultParagraphSeparator', false, 'p');

        el.addEventListener('input', () => {
            dotnetRef.invokeMethodAsync('OnHtmlChanged', el.innerHTML);
        });

        el.addEventListener('paste', (e) => {
            e.preventDefault();
            const text = (e.clipboardData || window.clipboardData).getData('text/plain');
            document.execCommand('insertText', false, text);
        });
    }

    function focusEditor(id) {
        const el = document.getElementById(id);
        if (el) {
            el.focus();
        }
    }

    function exec(id, command, value) {
        focusEditor(id);
        document.execCommand(command, false, value ?? null);
        const el = document.getElementById(id);
        const ref = editors[id];
        if (el && ref) {
            ref.invokeMethodAsync('OnHtmlChanged', el.innerHTML);
        }
    }

    function formatBlock(id, tag) {
        exec(id, 'formatBlock', tag);
    }

    function insertLink(id) {
        const url = window.prompt('Lenke (URL):', 'https://');
        if (!url) {
            return;
        }
        exec(id, 'createLink', url);
    }

    function setHtml(id, html) {
        const el = document.getElementById(id);
        if (el && el.innerHTML !== html) {
            el.innerHTML = html || '';
        }
    }

    return { init, exec, formatBlock, insertLink, setHtml, focusEditor };
})();
