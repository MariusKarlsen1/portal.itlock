// Lite, frittstående Leaflet-kart til sidepaneler (f.eks. Lokasjon i
// kalenderens detaljpanel). Egen fil fordi window.kart holder ÉN delt
// kartinstans og ville blitt revet ned av hovedkartet på /kart.
window.minikart = (function () {
    const kart = {};

    function vis(elementId, lat, lng, farge) {
        const el = document.getElementById(elementId);
        if (!el || typeof L === 'undefined') {
            return;
        }

        if (kart[elementId]) {
            kart[elementId].remove();
            delete kart[elementId];
        }

        const map = L.map(elementId, {
            zoomControl: false,
            attributionControl: false,
            dragging: false,
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            touchZoom: false
        }).setView([lat, lng], 15);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19 }).addTo(map);

        L.marker([lat, lng], {
            icon: L.divIcon({
                className: 'minikart-pin',
                html: `<svg width="26" height="35" viewBox="0 0 28 38" style="filter:drop-shadow(0 2px 3px rgba(0,0,0,.35))">
                    <path d="M14 1C6.8 1 1 6.8 1 14c0 9.5 11.3 21.6 12.2 22.6.4.4 1.1.4 1.5 0C15.7 35.6 27 23.5 27 14 27 6.8 21.2 1 14 1Z" fill="${farge || '#c0392b'}" stroke="#fff" stroke-width="2"/>
                    <circle cx="14" cy="14" r="5.5" fill="#fff"/>
                </svg>`,
                iconSize: [26, 35],
                iconAnchor: [13, 34]
            })
        }).addTo(map);

        // Kartet tegnes i en beholder som akkurat har blitt synlig, så
        // Leaflet må måle den på nytt for å unngå grå felter.
        setTimeout(function () { map.invalidateSize(); }, 60);

        kart[elementId] = map;
    }

    function fjern(elementId) {
        if (kart[elementId]) {
            kart[elementId].remove();
            delete kart[elementId];
        }
    }

    return { vis, fjern };
})();
