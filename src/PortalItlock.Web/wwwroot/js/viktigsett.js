// Viktige kunngjøringer (VK4): når et innlegg med data-viktig="<id>" har vært minst
// 60 % synlig på skjermen i 2 sekunder, sier vi fra til komponenten, som lagrer at
// brukeren har sett det. Ingen knapp å trykke.
window.viktigSett = (function () {
    var ref = null;
    var obs = null;
    var timere = new Map();

    function sjekk(oppforinger) {
        oppforinger.forEach(function (e) {
            var el = e.target;
            var id = parseInt(el.getAttribute('data-viktig'), 10);
            if (!id) {
                return;
            }
            // Lange innlegg (høyere enn skjermen) når aldri 60 % - da holder det at
            // innlegget fyller minst halve skjermen.
            var synlig = e.isIntersecting && (e.intersectionRatio >= 0.6
                || (e.rootBounds && e.intersectionRect.height >= e.rootBounds.height * 0.5));
            if (synlig) {
                if (!timere.has(el)) {
                    timere.set(el, setTimeout(function () {
                        timere.delete(el);
                        obs.unobserve(el);
                        el.__viktigSett = true;
                        if (ref) {
                            ref.invokeMethodAsync('MarkerSett', id);
                        }
                    }, 2000));
                }
            } else if (timere.has(el)) {
                clearTimeout(timere.get(el));
                timere.delete(el);
            }
        });
    }

    return {
        start: function (dotnetRef) {
            ref = dotnetRef;
            if (!obs && 'IntersectionObserver' in window) {
                obs = new IntersectionObserver(sjekk, { threshold: [0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.8, 1] });
            }
        },
        // Kalles etter hver rendring: begynner å følge med på nye viktige innlegg.
        oppdater: function () {
            if (!obs) {
                return;
            }
            document.querySelectorAll('[data-viktig]').forEach(function (el) {
                if (!el.__viktigFulgt && !el.__viktigSett) {
                    el.__viktigFulgt = true;
                    obs.observe(el);
                }
            });
        },
        stopp: function () {
            if (obs) {
                obs.disconnect();
                obs = null;
            }
            timere.forEach(function (t) { clearTimeout(t); });
            timere.clear();
            ref = null;
        }
    };
})();
