// Enkel tekstredigering for sluttrapporten (RikTekstFelt.razor): fet, kursiv,
// understrek, tre faste tekststørrelser (avsnitt/stor/overskrift) og lister.
// Faste størrelser i stedet for fri px-størrelse, så teksten ser lik ut på
// skjermen og i rapport-PDF-en. Innholdet sendes tilbake til Blazor som HTML
// og renses på serveren (RikTekst.Rens).
window.rikTekst = (function () {
    const felt = new Map();

    function send(el) {
        const ref = felt.get(el);
        if (ref) {
            ref.invokeMethodAsync('Endret', el.innerHTML);
        }
    }

    function init(el, ref, html) {
        if (!el) {
            return;
        }

        felt.set(el, ref);
        el.innerHTML = html || '';
        document.execCommand('defaultParagraphSeparator', false, 'p');
        let tidtaker = null;

        el.addEventListener('input', () => {
            clearTimeout(tidtaker);
            tidtaker = setTimeout(() => send(el), 700);
        });
        el.addEventListener('blur', () => {
            clearTimeout(tidtaker);
            send(el);
        });
        // Lim inn som ren tekst, så det ikke følger med farger og skrifter fra andre steder.
        el.addEventListener('paste', (e) => {
            e.preventDefault();
            const tekst = (e.clipboardData || window.clipboardData).getData('text/plain');
            document.execCommand('insertText', false, tekst);
        });
    }

    function kommando(el, cmd, verdi) {
        if (!el) {
            return;
        }

        el.focus();
        document.execCommand('styleWithCSS', false, false);
        document.execCommand('defaultParagraphSeparator', false, 'p');
        document.execCommand(cmd, false, verdi || null);
        send(el);
    }

    function fjern(el) {
        felt.delete(el);
    }

    return { init, kommando, fjern };
})();
