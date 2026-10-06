window.kart = (function () {
    let map = null;
    let markers = [];
    let markersById = {};

    function lagIkon(farge) {
        return L.divIcon({
            className: 'kart-punkt-ikon',
            html: `<span style="display:block;width:18px;height:18px;border-radius:50%;background:${farge || '#2f6fb3'};border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.45)"></span>`,
            iconSize: [18, 18],
            iconAnchor: [9, 9],
            popupAnchor: [0, -9]
        });
    }

    // Dråpeformet markør (i stedet for prikken over) - brukes når punktet har
    // pin:true, f.eks. Arbeidsordre sin kartvisning.
    function lagPinIkon(farge) {
        const f = farge || '#2f6fb3';
        return L.divIcon({
            className: 'kart-punkt-ikon',
            html: `<svg width="28" height="38" viewBox="0 0 28 38" style="filter:drop-shadow(0 2px 3px rgba(0,0,0,.35))">
                <path d="M14 1C6.8 1 1 6.8 1 14c0 9.5 11.3 21.6 12.2 22.6.4.4 1.1.4 1.5 0C15.7 35.6 27 23.5 27 14 27 6.8 21.2 1 14 1Z" fill="${f}" stroke="#fff" stroke-width="2"/>
                <circle cx="14" cy="14" r="5.5" fill="#fff"/>
            </svg>`,
            iconSize: [28, 38],
            iconAnchor: [14, 37],
            popupAnchor: [0, -36]
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
        if (p.rikPopup) {
            return lagRikPopup(p);
        }
        const status = p.status ? `<br><em>${p.status}</em>` : '';
        return `<strong>${p.tittel}</strong><br>${p.undertekst ?? ''}${status}`;
    }

    // Rikere popup-kort (Arbeidsordre sin kartvisning) - statuspunkt+tittel,
    // adresse, status/frist/timer-badger og en lenke til detaljsiden.
    function lagRikPopup(p) {
        const badges = [];
        if (p.statusTekst) {
            badges.push(`<span style="display:inline-flex;align-items:center;gap:4px;font-size:11px;font-weight:600;padding:2px 8px;border-radius:999px;background:${p.statusBg || '#eee'};color:${p.farge || '#333'}">${p.statusTekst}</span>`);
        }
        if (p.datoTekst) {
            badges.push(`<span style="font-size:11px;color:#6b6863">${p.datoTekst}</span>`);
        }
        if (p.timerTekst) {
            badges.push(`<span style="font-size:11px;color:#6b6863">${p.timerTekst}</span>`);
        }

        const montor = p.undertekst2
            ? `<div style="font-size:12px;color:#6b6863;margin-top:6px">${p.undertekst2}</div>`
            : '';

        const lenke = p.href
            ? `<a href="${p.href}" style="display:block;text-align:center;margin-top:10px;padding:7px 0;border:1px solid #ddd6cb;border-radius:8px;color:#292927;text-decoration:none;font-size:12.5px;font-weight:600">Vis detaljer</a>`
            : '';

        return `<div style="min-width:200px">
            <div style="display:flex;align-items:center;gap:6px;font-weight:700;font-size:13.5px">
                <span style="width:8px;height:8px;border-radius:50%;background:${p.farge || '#333'};flex-shrink:0"></span>
                <span>${p.tittel}</span>
            </div>
            <div style="font-size:12px;color:#6b6863;margin-top:3px">${p.undertekst ?? ''}</div>
            <div style="display:flex;flex-wrap:wrap;align-items:center;gap:6px;margin-top:8px">${badges.join('')}</div>
            ${montor}
            ${lenke}
        </div>`;
    }

    function tegnMarkorer(punkter) {
        markers.forEach(m => map.removeLayer(m));
        markers = [];
        markersById = {};

        (punkter || []).forEach(p => {
            const ikon = p.pin ? lagPinIkon(p.farge) : lagIkon(p.farge);
            const marker = L.marker([p.lat, p.lng], { icon: ikon }).addTo(map);
            marker.bindPopup(lagPopup(p), { minWidth: 220 });
            markers.push(marker);
            if (p.id !== undefined && p.id !== null) {
                markersById[p.id] = marker;
            }
        });
    }

    // Panorerer/zoomer til punktet med gitt id (sendt inn som p.id fra siden),
    // åpner popup-en og lar markøren pulsere et par ganger - brukt når man
    // klikker et kort i en liste ved siden av et kart, for å vise hvor det
    // kortet faktisk er uten å måtte lete etter det selv.
    function fremhev(id) {
        const marker = markersById[id];
        if (!marker || !map) {
            return;
        }

        const malZoom = Math.max(map.getZoom(), 16);
        map.flyTo(marker.getLatLng(), malZoom, { duration: 0.6 });
        marker.openPopup();

        const el = marker.getElement();
        if (el) {
            el.classList.remove('kart-punkt-blink');
            void el.offsetWidth; // tvinger reflow så animasjonen starter på nytt ved gjentatte klikk
            el.classList.add('kart-punkt-blink');
            setTimeout(() => el.classList.remove('kart-punkt-blink'), 1800);
        }
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

    return { init, setPunkter, oppdaterPunkter, fremhev };
})();
