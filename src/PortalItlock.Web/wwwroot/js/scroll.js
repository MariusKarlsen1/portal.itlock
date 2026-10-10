// Blazor fanger opp klikk på <a href="#id"> og ruter dem gjennom
// NavigationManager, som ikke scroller til fragmentet når man allerede står
// på siden. Knapper som skal hoppe til en seksjon kaller derfor denne i
// stedet. Ligger i egen fil fordi <script> inne i en komponent ikke kjøres
// når komponenten rendres av Blazor.
window.scrollTilElement = function (id) {
    var el = document.getElementById(id);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
};
