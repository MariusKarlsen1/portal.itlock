window.kart = (function () {
    let map = null;
    let markers = [];

    function lagIkon(farge) {
        return L.divIcon({
            className: 'kart-punkt-ikon',
            html: `<span style="display:block;width:18px;height:18px;border-radius:50%;background:${farge || '#2f6fb3'};border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.45)"></span>`,
            iconSize: [18, 18],
            iconAnchor: [9, 9],
            popupAnchor: [0, -9]
        });
    }

    function init(elementId, punkter) {
        const el = document.getElementById(elementId);
        if (!el || typeof L === 'undefined') {
            return;
        }

        if (map) {
            map.remove();
            map = null;
        }
        markers = [];

        map = L.map(elementId);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap-bidragsytere',
            maxZoom: 19
        }).addTo(map);

        setPunkter(punkter);
    }

    function lagPopup(p) {
        const status = p.status ? `<br><em>${p.status}</em>` : '';
        return `<strong>${p.tittel}</strong><br>${p.undertekst ?? ''}${status}`;
    }

    function tegnMarkorer(punkter) {
        markers.forEach(m => map.removeLayer(m));
        markers = [];

        (punkter || []).forEach(p => {
            const marker = L.marker([p.lat, p.lng], { icon: lagIkon(p.farge) }).addTo(map);
            marker.bindPopup(lagPopup(p));
            markers.push(marker);
        });
    }

    function setPunkter(punkter) {
        if (!map) {
            return;
        }

        tegnMarkorer(punkter);

        if (!punkter || punkter.length === 0) {
            map.setView([59.9139, 10.7522], 6);
            return;
        }

        const bounds = punkter.map(p => [p.lat, p.lng]);
        if (bounds.length === 1) {
            map.setView(bounds[0], 14);
        } else {
            map.fitBounds(bounds, { padding: [30, 30] });
        }
    }

    function oppdaterPunkter(punkter) {
        if (!map) {
            return;
        }

        tegnMarkorer(punkter);
    }

    return { init, setPunkter, oppdaterPunkter };
})();
