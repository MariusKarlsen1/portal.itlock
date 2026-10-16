// Lukker apne nedtrekk i toppfeltet nar man trykker hvor som helst ellers pa
// siden, i stedet for a kreve at man treffer samme knapp en gang til.
//
// @onclick:stopPropagation i TopBar.razor stopper bare Blazor sine egne
// hendelser, ikke vanlige DOM-lyttere som denne. Klikk INNE i nedtrekkene
// (varsel-wrap, notater-wrap, bruker-meny-wrap, sok-wrap) ma derfor sorteres
// bort her - ellers lukket f.eks. "Tilpass meny" hele brukermenyen.
//
// Lytteren legges pa bare mens noe er apent, slik at vi ikke sender et kall
// over SignalR for hvert eneste klikk i portalen.
window.utenforKlikk = (function () {
    let handler = null;

    const INNE = '.varsel-wrap, .notater-wrap, .bruker-meny-wrap, .sok-wrap';

    function lytt(dotNetRef) {
        stopp();

        handler = function (e) {
            if (e.target instanceof Element && e.target.closest(INNE)) {
                return;
            }
            dotNetRef.invokeMethodAsync('LukkAlleNedtrekk');
        };

        // Litt forsinket pastart: klikket som APNET menyen er fortsatt pa vei
        // opp gjennom DOM-en, og ville ellers lukket den med en gang.
        setTimeout(function () {
            if (handler) {
                document.addEventListener('click', handler);
            }
        }, 0);
    }

    function stopp() {
        if (handler) {
            document.removeEventListener('click', handler);
            handler = null;
        }
    }

    return { lytt, stopp };
})();
