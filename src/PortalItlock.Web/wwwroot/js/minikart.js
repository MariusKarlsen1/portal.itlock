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

    // Serviceavtaler (S9-I): gjøremålene som nummererte runder i statusfarge,
    // de andre avtalene som små prikker. Kartet zoomes til alle punktene.
    // punkter: [{ lat, lng, farge, nr (null = ingen nummer), tittel, href }]
    function visAvtaler(elementId, punkter) {
        const el = document.getElementById(elementId);
        if (!el || typeof L === 'undefined' || !punkter || punkter.length === 0) {
            return;
        }

        if (kart[elementId]) {
            kart[elementId].remove();
            delete kart[elementId];
        }

        const map = L.map(elementId, {
            zoomControl: true,
            attributionControl: false,
            scrollWheelZoom: false
        });
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19 }).addTo(map);

        // Prikkene først, så de nummererte ligger øverst.
        const sortert = punkter.slice().sort((a, b) => (a.nr ? 1 : 0) - (b.nr ? 1 : 0));
        sortert.forEach(function (p) {
            const html = p.nr
                ? `<span style="display:grid;place-items:center;width:24px;height:24px;border-radius:50%;background:${p.farge};color:#fff;font:800 12px Inter,Arial,sans-serif;border:2px solid #fff;box-shadow:0 2px 6px rgba(0,0,0,.35)">${p.nr}</span>`
                : `<span style="display:block;width:12px;height:12px;border-radius:50%;background:${p.farge};border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.3)"></span>`;
            const storrelse = p.nr ? 24 : 12;
            const m = L.marker([p.lat, p.lng], {
                icon: L.divIcon({ className: 'minikart-pin', html: html, iconSize: [storrelse, storrelse], iconAnchor: [storrelse / 2, storrelse / 2] }),
                zIndexOffset: p.nr ? 1000 : 0
            }).addTo(map);
            if (p.tittel) {
                m.bindTooltip(p.tittel, { direction: 'top', offset: [0, -storrelse / 2] });
            }
            if (p.href) {
                m.on('click', function () { window.location.href = p.href; });
            }
        });

        const grenser = L.latLngBounds(punkter.map(function (p) { return [p.lat, p.lng]; }));
        if (punkter.length === 1) {
            map.setView(grenser.getCenter(), 12);
        } else {
            map.fitBounds(grenser, { padding: [28, 28], maxZoom: 13 });
        }

        setTimeout(function () { map.invalidateSize(); }, 60);
        kart[elementId] = map;
    }

    function fjern(elementId) {
        if (kart[elementId]) {
            kart[elementId].remove();
            delete kart[elementId];
        }
    }

    return { vis, visAvtaler, fjern };
})();
