window.richtext = (function () {
    const editors = {};
    const savedRanges = {};
    const FONT_SIZES = [12, 14, 16, 18, 20, 24, 28, 32, 40];

    let floatingBar = null;
    let activeId = null;

    // Et klikk i en verktøylinje (flytende eller fast) kan kollapse tekstmarkeringen
    // (selv om vi hindrer at fokus flytter seg), så vi lagrer Range på mousedown og
    // bruker den lagrede Range'n direkte når formateringen skal utføres.
    function saveRangeFor(id) {
        const sel = window.getSelection();
        if (sel && sel.rangeCount > 0 && !sel.isCollapsed) {
            savedRanges[id] = sel.getRangeAt(0).cloneRange();
        }
    }

    function restoreSelectionAndFocus(id) {
        const range = savedRanges[id];
        if (!range) {
            return;
        }
        // Fokus må settes FØR Range gjenopprettes - ellers kan selve fokusbyttet til
        // redigeringsfeltet nullstille markeringen vi nettopp satte tilbake.
        focusEditor(id);
        const sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(range);
    }

    // Skriftstørrelse settes ved å pakke inn den lagrede Range'n direkte i et <span>
    // med inline style, i stedet for å bruke execCommand('fontSize', ...). Native
    // <select>-elementer stjeler fokus når nedtrekksmenyen åpnes/lukkes, og
    // execCommand er upålitelig i den situasjonen - direkte DOM-manipulasjon av den
    // lagrede Range'n er upåvirket av hvor fokus befinner seg.
    function applyFontSizeToRange(id, px) {
        const range = savedRanges[id];
        const el = document.getElementById(id);
        if (!range || !el) {
            return;
        }

        const span = document.createElement('span');
        span.style.fontSize = px + 'px';
        try {
            range.surroundContents(span);
        } catch (e) {
            // Range spenner over flere elementer - surroundContents feiler da.
            const content = range.extractContents();
            span.appendChild(content);
            range.insertNode(span);
        }

        const ref = editors[id];
        if (ref) {
            ref.invokeMethodAsync('OnHtmlChanged', el.innerHTML);
        }
    }

    function wireSizeSelect(selectEl, id) {
        // Ikke preventDefault her - det ville hindret nedtrekksmenyen fra å åpne seg.
        selectEl.addEventListener('mousedown', () => saveRangeFor(id));
        selectEl.addEventListener('change', (e) => {
            const px = e.target.value;
            if (px) {
                applyFontSizeToRange(id, px);
            }
            e.target.value = '';
        });
    }

    function ensureFloatingBar() {
        if (floatingBar) {
            return floatingBar;
        }

        floatingBar = document.createElement('div');
        floatingBar.className = 'rte-floating-bar';

        const sizeOptions = FONT_SIZES.map(s => `<option value="${s}">${s}</option>`).join('');
        floatingBar.innerHTML = `
            <button type="button" data-cmd="bold" title="Fet skrift"><b>F</b></button>
            <button type="button" data-cmd="italic" title="Kursiv"><i>K</i></button>
            <select data-cmd="size" title="Skriftstørrelse">
                <option value="">Str.</option>
                ${sizeOptions}
            </select>`;
        floatingBar.style.display = 'none';

        floatingBar.querySelectorAll('button').forEach((btn) => {
            // preventDefault hindrer at knappen stjeler fokus/kollapser markeringen.
            btn.addEventListener('mousedown', (e) => {
                e.preventDefault();
                if (activeId) {
                    saveRangeFor(activeId);
                }
            });
        });

        const sizeSelect = floatingBar.querySelector('[data-cmd="size"]');
        sizeSelect.addEventListener('mousedown', () => {
            if (activeId) {
                saveRangeFor(activeId);
            }
        });

        floatingBar.querySelector('[data-cmd="bold"]').addEventListener('click', () => {
            if (activeId) {
                restoreSelectionAndFocus(activeId);
                exec(activeId, 'bold');
            }
        });
        floatingBar.querySelector('[data-cmd="italic"]').addEventListener('click', () => {
            if (activeId) {
                restoreSelectionAndFocus(activeId);
                exec(activeId, 'italic');
            }
        });
        sizeSelect.addEventListener('change', (e) => {
            const px = e.target.value;
            if (activeId && px) {
                applyFontSizeToRange(activeId, px);
            }
            e.target.value = '';
        });

        document.body.appendChild(floatingBar);

        document.addEventListener('mousedown', (e) => {
            if (!floatingBar.contains(e.target)) {
                floatingBar.style.display = 'none';
            }
        });

        return floatingBar;
    }

    function updateFloatingBarPosition(id, el) {
        const sel = window.getSelection();
        const bar = ensureFloatingBar();

        if (!sel || sel.rangeCount === 0 || sel.isCollapsed || !el.contains(sel.anchorNode)) {
            bar.style.display = 'none';
            return;
        }

        activeId = id;
        const rect = sel.getRangeAt(0).getBoundingClientRect();
        bar.style.display = 'flex';
        bar.style.top = Math.max(4, window.scrollY + rect.top - bar.offsetHeight - 6) + 'px';
        bar.style.left = Math.max(4, window.scrollX + rect.left + rect.width / 2 - bar.offsetWidth / 2) + 'px';
    }

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

        el.addEventListener('mouseup', () => setTimeout(() => updateFloatingBarPosition(id, el), 0));
        el.addEventListener('keyup', () => setTimeout(() => updateFloatingBarPosition(id, el), 0));

        const toolbarSizeSelect = document.getElementById(id + '-size');
        if (toolbarSizeSelect) {
            wireSizeSelect(toolbarSizeSelect, id);
        }
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
