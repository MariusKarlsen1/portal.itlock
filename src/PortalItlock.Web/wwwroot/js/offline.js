// Registrerer service worker for installerbar app (PWA) + statisk cache, og viser en
// enkel "frakoblet"-banner nederst på skjermen basert på nettleserens online/offline-events.
// Kjører helt uavhengig av Blazor, siden Blazor Server selv ikke fungerer uten tilkobling.
(function () {
    if ('serviceWorker' in navigator) {
        // Når en ny service-worker overtar kontrollen (etter at den har
        // ryddet bort gamle bufre, se service-worker.js), er siden vi
        // allerede står på fortsatt lastet med det gamle, bufrede innholdet.
        // Laster derfor siden på nytt automatisk denne ene gangen, slik at
        // man slipper å måtte åpne appen flere ganger for at rettelser skal
        // slå gjennom. Vokteren hindrer en evig reload-løkke.
        var harReloadet = false;
        navigator.serviceWorker.addEventListener('controllerchange', function () {
            if (harReloadet) {
                return;
            }
            harReloadet = true;
            window.location.reload();
        });

        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/service-worker.js').then(function (reg) {
                // Tving en sjekk mot nettverket for en ny service-worker-fil hver
                // gang appen åpnes, i stedet for å stole på nettleserens egen
                // (ofte forsinkede) periodiske sjekk - slik slår rettelser i
                // service-worker.js (f.eks. nye bufferversjoner) raskere
                // gjennom på telefoner som allerede har appen installert.
                reg.update();
            }).catch(function () { });
        });
    }

    function lagBanner() {
        if (document.getElementById('itlock-offline-banner')) {
            return document.getElementById('itlock-offline-banner');
        }
        var banner = document.createElement('div');
        banner.id = 'itlock-offline-banner';
        banner.textContent = '📴 Ingen tilkobling – viser sist lagrede data';
        banner.style.cssText = 'position:fixed;left:0;right:0;bottom:0;z-index:2000;' +
            'background:#835e41;color:#fff;text-align:center;padding:0.5rem 1rem;' +
            'font:600 0.85rem system-ui,sans-serif;display:none;';
        document.body.appendChild(banner);
        return banner;
    }

    function oppdaterStatus() {
        var banner = lagBanner();
        banner.style.display = navigator.onLine ? 'none' : 'block';
    }

    window.addEventListener('online', oppdaterStatus);
    window.addEventListener('offline', oppdaterStatus);
    document.addEventListener('DOMContentLoaded', oppdaterStatus);
})();
