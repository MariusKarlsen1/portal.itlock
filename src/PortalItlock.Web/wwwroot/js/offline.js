// Registrerer service worker for installerbar app (PWA) + statisk cache, og viser en
// enkel "frakoblet"-banner nederst på skjermen basert på nettleserens online/offline-events.
// Kjører helt uavhengig av Blazor, siden Blazor Server selv ikke fungerer uten tilkobling.
(function () {
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/service-worker.js').catch(function () { });
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
