// Lukker apne nedtrekk i toppfeltet nar man trykker hvor som helst ellers pa
// siden, i stedet for a kreve at man treffer samme knapp en gang til.
//
// Selve nedtrekkene (varsel-wrap, notater-wrap, bruker-meny-wrap, sok-wrap i
// TopBar.razor) har @onclick:stopPropagation, sa klikk INNE i dem naar aldri
// hit - lytteren her far dermed bare de klikkene som faktisk er utenfor.
//
// Lytteren legges pa bare mens noe er apent, slik at vi ikke sender et kall
// over SignalR for hvert eneste klikk i portalen.
window.utenforKlikk = (function () {
    let handler = null;

    function lytt(dotNetRef) {
        stopp();

        handler = function () {
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
