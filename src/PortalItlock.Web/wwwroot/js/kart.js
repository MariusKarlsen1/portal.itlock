window.kart = (function () {
    let map = null;
    let markers = [];
    let markersById = {};
    let flate = null;
    let dotNet = null;
    let klyngelag = null;

    const flater = {
        kart: {
            url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            opts: { attribution: '&copy; OpenStreetMap-bidragsytere', maxZoom: 19 }
        },
        satellitt: {
            url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
            opts: { attribution: '&copy; Esri', maxZoom: 19 }
        }
    };

    function lagIkon(farge) {
        return L.divIcon({
            className: 'kart-punkt-ikon',
            html: `<span style="display:block;width:18px;height:18px;border-radius:50%;background:${farge || '#2f6fb3'};border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.45)"></span>`,
            iconSize: [18, 18],
            iconAnchor: [9, 9],
            popupAnchor: [0, -9]
        });
    }

    // Små hvite symboler inni markørene, så man ser hva slags jobb det er
    // uten å klikke. Tegnet i et 24x24-rutenett som skaleres ned.
    const glyfer = {
        tool: '<path d="M18.5 5.5a4.5 4.5 0 0 1-6 6l-5 5a2 2 0 1 1-3-3l5-5a4.5 4.5 0 0 1 6-6l-2.6 2.6 2 2L17.5 4.5Z"/>',
        user: '<circle cx="12" cy="8.5" r="3.4"/><path d="M5.5 19c.6-3.4 3.3-5.2 6.5-5.2s5.9 1.8 6.5 5.2Z"/>',
        check: '<path d="M5.5 12.5 10 17l8.5-9.5-1.9-1.7L10 13.2l-2.7-2.6Z"/>',
        clock: '<path d="M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16Zm.9 8.3V7.4h-1.8v5.6l4 2.4.9-1.5Z"/>',
        alert: '<path d="M11.1 5h1.8v8.4h-1.8Zm0 10.2h1.8V18h-1.8Z"/>'
    };

    // Dråpeformet markør (i stedet for prikken over) - brukes når punktet har
    // pin:true, f.eks. Arbeidsordre sin kartvisning. glyph velger symbolet.
    function lagPinIkon(farge, glyph) {
        const f = farge || '#2f6fb3';
        const symbol = glyfer[glyph] || null;
        const innhold = symbol
            ? `<g transform="translate(6 6) scale(0.67)" fill="#fff">${symbol}</g>`
            : '<circle cx="15" cy="15" r="5.5" fill="#fff"/>';

        return L.divIcon({
            className: 'kart-punkt-ikon',
            html: `<svg width="30" height="40" viewBox="0 0 30 40" style="filter:drop-shadow(0 2px 4px rgba(0,0,0,.35))">
                <path d="M15 1C7.8 1 2 6.8 2 14c0 9.9 11.6 23.1 12.3 23.9.4.4 1 .4 1.4 0C16.4 37.1 28 23.9 28 14 28 6.8 22.2 1 15 1Z" fill="${f}" stroke="#fff" stroke-width="2.5"/>
                ${innhold}
            </svg>`,
            iconSize: [30, 40],
            iconAnchor: [15, 39],
            popupAnchor: [0, -38],
            tooltipAnchor: [0, -34]
        });
    }

    // Infokortet som henger ved markøren i Arbeidsordre sin kartvisning -
    // vises permanent så lenge det er få nok markører til at de ikke
    // overlapper hverandre.
    function lagKallelut(p) {
        const linjer = [
            p.aoNr ? `<span class="kart-kallelut-nr">${p.aoNr}</span>` : '',
            p.prosjekt ? `<span class="kart-kallelut-prosjekt">${p.prosjekt}</span>` : '',
            `<span class="kart-kallelut-tittel">${p.tittel}</span>`,
            p.undertekst2 ? `<span class="kart-kallelut-person">${p.undertekst2}</span>` : ''
        ];
        return `<span class="kart-kallelut">${linjer.join('')}</span>`;
    }

    // dotNetRef er valgfri - sendes inn av sider som vil få beskjed når en
    // markør klikkes (Arbeidsordre sin kartvisning åpner detaljlinja under
    // kartet i stedet for å nøye seg med popup-en).
    function init(elementId, punkter, dotNetRef) {
        const el = document.getElementById(elementId);
        if (!el || typeof L === 'undefined') {
            return;
        }

        if (map) {
            map.remove();
            map = null;
        }
        markers = [];
        dotNet = dotNetRef || null;

        map = L.map(elementId, { zoomControl: false });
        settBakgrunn('kart');

        setPunkter(punkter);
    }

    function settBakgrunn(type) {
        if (!map) {
            return;
        }

        const valgt = flater[type] || flater.kart;
        if (flate) {
            map.removeLayer(flate);
        }
        flate = L.tileLayer(valgt.url, valgt.opts).addTo(map);
    }

    function zoom(delta) {
        if (map) {
            map.setZoom(map.getZoom() + delta);
        }
    }

    // Sentrerer på brukerens egen posisjon. Nettleseren spør om tillatelse
    // første gang; avslag håndteres stille (kartet står der det står).
    function finnMeg() {
        if (!map || !navigator.geolocation) {
            return;
        }

        navigator.geolocation.getCurrentPosition(function (pos) {
            map.flyTo([pos.coords.latitude, pos.coords.longitude], 14, { duration: 0.6 });
        }, function () { });
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

    // Grå klyngeboble med antall, slik referansedesignet viser det.
    function lagKlyngeIkon(klynge) {
        const antall = klynge.getChildCount();
        return L.divIcon({
            className: 'kart-klynge',
            html: `<span>${antall}</span>`,
            iconSize: [34, 34]
        });
    }

    function tegnMarkorer(punkter) {
        if (klyngelag) {
            map.removeLayer(klyngelag);
            klyngelag = null;
        }
        markers.forEach(m => map.removeLayer(m));
        markers = [];
        markersById = {};

        const liste = punkter || [];

        // Klynger krever markercluster-utvidelsen. Er den ikke lastet,
        // legges markørene rett på kartet som før.
        const brukKlynger = typeof L.markerClusterGroup === 'function';
        if (brukKlynger) {
            klyngelag = L.markerClusterGroup({
                iconCreateFunction: lagKlyngeIkon,
                showCoverageOnHover: false,
                maxClusterRadius: 48,
                spiderfyOnMaxZoom: true
            });
        }

        // Permanente infokort bare når det er få nok markører til at de ikke
        // dekker hverandre - ellers havner de oppå alt.
        const visKallelut = liste.length > 0 && liste.length <= 12;

        liste.forEach(p => {
            const ikon = p.pin ? lagPinIkon(p.farge, p.glyph) : lagIkon(p.farge);
            const marker = L.marker([p.lat, p.lng], { icon: ikon });
            marker.bindPopup(lagPopup(p), { minWidth: 220 });

            if (p.kallelut && visKallelut) {
                marker.bindTooltip(lagKallelut(p), {
                    permanent: true,
                    direction: 'top',
                    className: 'kart-kallelut-wrap',
                    opacity: 1
                });
            }

            if (dotNet && p.id !== undefined && p.id !== null) {
                marker.on('click', function () {
                    dotNet.invokeMethodAsync('OnKartPunktValgt', p.id);
                });
            }

            if (brukKlynger) {
                klyngelag.addLayer(marker);
            } else {
                marker.addTo(map);
            }

            markers.push(marker);
            if (p.id !== undefined && p.id !== null) {
                markersById[p.id] = marker;
            }
        });

        if (brukKlynger) {
            map.addLayer(klyngelag);
        }
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

        function blink() {
            marker.openPopup();
            const el = marker.getElement();
            if (el) {
                el.classList.remove('kart-punkt-blink');
                void el.offsetWidth; // tvinger reflow så animasjonen starter på nytt ved gjentatte klikk
                el.classList.add('kart-punkt-blink');
                setTimeout(() => el.classList.remove('kart-punkt-blink'), 1800);
            }
        }

        // Ligger markøren i en sammenslått klynge må den pakkes ut først,
        // ellers finnes den ikke i DOM-en og popup-en har ingenting å feste
        // seg til.
        if (klyngelag && typeof klyngelag.zoomToShowLayer === 'function') {
            klyngelag.zoomToShowLayer(marker, blink);
            return;
        }

        map.flyTo(marker.getLatLng(), Math.max(map.getZoom(), 16), { duration: 0.6 });
        blink();
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

    return { init, setPunkter, oppdaterPunkter, fremhev, settBakgrunn, zoom, finnMeg };
})();
