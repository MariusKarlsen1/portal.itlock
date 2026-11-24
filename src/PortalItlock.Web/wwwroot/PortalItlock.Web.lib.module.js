// Blazor kaller disse selv (JS-initialisering for PortalItlock.Web).
// afterServerStarted: den interaktive kretsen er oppe. lasting.js bruker
// flagget til å laste siden på nytt én gang hvis kretsen aldri kom i gang
// (siden så ferdig ut, men knapper, hurtigvalg og kart virket ikke).
export function afterServerStarted() {
    window.__fkKretsStartet = true;
}
