// Enkel service worker for Full Kontroll: gjør appen installerbar (PWA) og lar de mest
// brukte feltsidene (Min dag, Kart) fortsatt vise sist nedlastede innhold uten dekning.
// Blazor Server trenger en levende tilkobling for selve interaktiviteten - denne SW-en
// kan derfor ikke gjøre appen fullstendig offline-funksjonell, men sikrer at montører i
// felt fortsatt ser adresser, telefonnumre og varelister fra siste gang de hadde dekning.
// NB: bump disse versjonsnumrene (og ingenting annet trenger å endres) hver
// gang gamle bufrede sider/filer skal tvinges bort - se activate-lytteren
// under, som rydder bort alt som ikke matcher disse navnene nøyaktig. Dette
// var roten til at rettelser (som fjerning av AI-snarveien) ikke slo gjennom
// på telefoner som allerede hadde appen installert: /min-dag ble bufret med
// datidens CSS/JS-versjoner, og siden selve service-worker.js-filen ikke var
// endret, oppdaget ikke nettleseren at noe hadde endret seg.
const STATIC_CACHE = 'itlock-static-v2';
const PAGE_CACHE = 'itlock-sider-v2';
const OFFLINE_SIDER = ['/min-dag', '/kart'];
const GJELDENDE_CACHER = [STATIC_CACHE, PAGE_CACHE];

self.addEventListener('install', () => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(navn => Promise.all(
                navn.filter(n => !GJELDENDE_CACHER.includes(n)).map(n => caches.delete(n))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const req = event.request;
    if (req.method !== 'GET') {
        return;
    }

    const url = new URL(req.url);
    if (url.origin !== self.location.origin) {
        return;
    }

    // Statiske filer: cache-first (de har ?v=-versjonering, trygt å cache lenge). Lagringen
    // av et ferskt nettverkssvar skjer via waitUntil, ellers kan nettleseren drepe SW-en
    // før asynkron cache.put() rekker å fullføre.
    if (/\.(css|js|png|jpg|jpeg|svg|woff2?|ico)$/.test(url.pathname)) {
        event.respondWith(
            caches.open(STATIC_CACHE).then(cache =>
                cache.match(req).then(cached => {
                    const nettverk = fetch(req).then(resp => {
                        if (resp.ok) {
                            event.waitUntil(cache.put(req, resp.clone()));
                        }
                        return resp;
                    }).catch(() => cached);
                    return cached || nettverk;
                })
            )
        );
        return;
    }

    // Nøkkelsider for feltbruk: nettverk først, men lagre siste vellykkede visning
    // slik at siden fortsatt viser noe (adresser, telefonnumre) uten dekning.
    const erSideNavigasjon = req.mode === 'navigate' || req.destination === 'document';
    if (erSideNavigasjon && OFFLINE_SIDER.some(s => url.pathname.startsWith(s))) {
        event.respondWith(
            fetch(req).then(resp => {
                if (resp.ok) {
                    const kloning = resp.clone();
                    event.waitUntil(caches.open(PAGE_CACHE).then(cache => cache.put(req, kloning)));
                }
                return resp;
            }).catch(() =>
                caches.open(PAGE_CACHE).then(cache => cache.match(req)).then(cached =>
                    cached || new Response(
                        '<h1>Ingen tilkobling</h1><p>Denne siden er ikke lastet ned fra før, og du er offline nå. Prøv igjen når du har dekning.</p>',
                        { headers: { 'Content-Type': 'text/html; charset=utf-8' } }
                    )
                )
            )
        );
    }
});
