// Hvit pille øverst i midten (.fk-pille i app.css) der buene i logoen lyser
// i tur:
//  - «Henter prosjekter …» når man bytter side (Blazor enhanced navigation).
//    Vises først etter en liten pause, så raske sidebytter ikke blinker.
//  - «Tilkoblet igjen» i grønt et par sekunder når SignalR-kretsen er tilbake
//    etter et brudd (selve «Kobler til på nytt»-pillen er
//    #components-reconnect-modal i App.razor, som Blazor viser/skjuler selv).
//  - Mislykket/avvist gjenoppkobling laster siden på nytt, så man aldri blir
//    stående i en død krets uten tilbakemelding.
(function () {
    var pille = null, forsinkelse = null, skjul = null;

    function lagPille() {
        if (!pille) {
            pille = document.createElement('div');
            pille.className = 'fk-pille';
            pille.setAttribute('role', 'status');
            document.body.appendChild(pille);
        }
        return pille;
    }

    function ring() {
        var r = document.querySelector('#components-reconnect-modal .fk-logo');
        return r ? r.outerHTML : '';
    }

    function vis(html, klasse, varighet) {
        var p = lagPille();
        clearTimeout(skjul);
        p.className = 'fk-pille vis' + (klasse ? ' ' + klasse : '');
        p.innerHTML = html;
        // Sikkerhetsnett: skjules uansett (f.eks. hvis lenken var en
        // filnedlasting og enhancedload derfor aldri kommer).
        skjul = setTimeout(ferdig, varighet || 10000);
    }

    function ferdig() {
        clearTimeout(forsinkelse);
        clearTimeout(skjul);
        if (pille && pille.className.indexOf('fk-pille-ok') < 0) {
            pille.className = 'fk-pille';
        }
    }

    function tekst(s) {
        var d = document.createElement('span');
        d.textContent = s;
        return d.innerHTML;
    }

    // «Henter prosjekter …» for menypunkter, ellers «Laster siden …».
    function navnFra(a) {
        if (!a.closest('.sidebar, .nav-scrollable')) {
            return 'Laster siden';
        }
        var t = (a.textContent || '').replace(/\s+/g, ' ').trim();
        if (!t || t.length > 32) {
            return 'Laster siden';
        }
        if (/^[A-ZÆØÅ][a-zæøå]/.test(t)) {
            t = t.charAt(0).toLowerCase() + t.slice(1);
        }
        return 'Henter ' + t;
    }

    document.addEventListener('click', function (e) {
        if (e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) {
            return;
        }
        var a = e.target.closest ? e.target.closest('a[href]') : null;
        if (!a || (a.target && a.target !== '_self') || a.hasAttribute('download')) {
            return;
        }
        var href = a.getAttribute('href') || '';
        if (href.charAt(0) === '#' || /^(javascript|mailto|tel):/i.test(href)) {
            return;
        }
        var url;
        try { url = new URL(a.href, location.href); } catch (_) { return; }
        if (url.origin !== location.origin || (url.pathname === location.pathname && url.search === location.search)) {
            return;
        }
        var navn = navnFra(a);
        clearTimeout(forsinkelse);
        forsinkelse = setTimeout(function () {
            vis(ring() + tekst(navn) + ' …');
        }, 250);
    }, true);

    if (window.Blazor && Blazor.addEventListener) {
        Blazor.addEventListener('enhancedload', ferdig);
    }
    window.addEventListener('pageshow', ferdig);

    var modal = document.getElementById('components-reconnect-modal');
    if (!modal) {
        return;
    }
    var harLastetPaNytt = false, varBrutt = false;
    new MutationObserver(function () {
        var k = modal.classList;
        if (k.contains('components-reconnect-failed') || k.contains('components-reconnect-rejected')) {
            if (!harLastetPaNytt) {
                harLastetPaNytt = true;
                window.location.reload();
            }
            return;
        }
        if (k.contains('components-reconnect-show')) {
            varBrutt = true;
            ferdig();
        } else if (varBrutt && k.contains('components-reconnect-hide')) {
            varBrutt = false;
            vis('<i>✓</i>Tilkoblet igjen', 'fk-pille-ok', 2500);
            setTimeout(function () { if (pille) { pille.className = 'fk-pille'; } }, 2500);
        }
    }).observe(modal, { attributes: true, attributeFilter: ['class'] });
})();
