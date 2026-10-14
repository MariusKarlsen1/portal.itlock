// Innlogging og passordsider (InnloggingRamme.razor): hilsen, tema, vis passord,
// passordstyrke og de små animasjonene til venstre.
(function () {
    var side = document.getElementById('liSide');
    if (!side) return;
    var rolig = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // ---------- hilsen etter tid på døgnet ----------
    var h = document.getElementById('liHilsen');
    if (h && h.getAttribute('data-tid')) {
        var t = new Date().getHours();
        h.textContent = t < 5 ? 'God natt.' : t < 10 ? 'God morgen.' : t < 17 ? 'God dag.' : 'God kveld.';
    }

    // ---------- lys/mørk modus (huskes i nettleseren) ----------
    try { if (localStorage.getItem('fk-login-tema') === 'mork') side.classList.add('li-mork'); } catch (e) { }
    var tema = document.getElementById('liTema');
    if (tema) tema.onclick = function () { var m = side.classList.toggle('li-mork'); try { localStorage.setItem('fk-login-tema', m ? 'mork' : 'lys'); } catch (e) { } };

    // ---------- vis/skjul passord ----------
    document.querySelectorAll('.li-oye').forEach(function (b) {
        b.onclick = function () {
            var p = document.getElementById(b.getAttribute('data-for'));
            if (p) p.type = p.type === 'password' ? 'text' : 'password';
        };
    });

    // Mobilvisning skal alltid starte på Oppgaver, uansett hvilken side
    // man ev. ble sendt til /login fra (returnUrl). Tømmer feltet på
    // mobil slik at serveren faller tilbake til "/", som i sin tur
    // sender videre til /min-dag (se den blokkerende sjekken i App.razor).
    if (window.matchMedia('(max-width: 640.98px)').matches) {
        var felt = document.querySelector('input[name="returnUrl"]');
        if (felt) felt.value = '';
    }

    var scene = document.getElementById('liScene');
    var status = document.getElementById('dsStatus');
    var statusTekst = document.getElementById('dsStatusTekst');
    function settStatus(tekst, klasse) {
        if (!status) return;
        statusTekst.textContent = tekst;
        status.className = 'li-status' + (klasse ? ' ' + klasse : '');
    }
    var tidsur = [];
    function etter(ms, f) { tidsur.push(setTimeout(f, ms)); }
    function stopp() { tidsur.forEach(clearTimeout); tidsur = []; }

    // =====================================================================
    // Dørscene: nøkkelkortet føres rolig mot leseren, og døra går opp
    // =====================================================================
    if (scene && scene.classList.contains('ds')) {
        var navn = document.getElementById('dsNavn');
        var epost = document.querySelector('input[type="email"]');

        // "thomas.eide@firma.no" -> "Thomas Eide" på kortet
        var standardNavn = navn ? navn.textContent : '';
        var oppdaterNavn = function () {
            if (!navn || !epost) return;
            var lokal = epost.value.split('@')[0].replace(/[0-9]+/g, ' ');
            var n = lokal.split(/[._\-+ ]+/).filter(Boolean).map(function (d) { return d.charAt(0).toUpperCase() + d.slice(1); }).join(' ');
            navn.textContent = n || standardNavn;
        };
        if (epost) { epost.addEventListener('input', oppdaterNavn); oppdaterNavn(); }

        var tilstand = function (k) {
            scene.classList.remove('skann', 'les', 'apen', 'nekt', 'klar');
            if (k) k.split(' ').forEach(function (x) { scene.classList.add(x); });
        };

        var venter = false;
        var runde = function () {
            stopp();
            tilstand('');
            settStatus('Låst');
            etter(1500, function () { tilstand('skann les'); });
            etter(2700, function () { tilstand('skann apen'); settStatus('Adgang innvilget', 'ok'); });
            etter(7200, function () { tilstand(''); settStatus('Låst'); });
            etter(11000, function () { if (!venter) runde(); });
        };

        // Ved innsending: kortet føres inn og døra går opp (skjemaet sendes som normalt)
        var skjema = document.querySelector('.li-skjema form');
        if (skjema) skjema.addEventListener('submit', function () {
            venter = true; stopp();
            tilstand('skann apen');
            settStatus('Velkommen inn', 'ok');
        });

        if (rolig) { settStatus('Full Kontroll'); }
        else if (side.getAttribute('data-feil')) {
            tilstand('nekt');
            settStatus('Feil passord – prøv igjen', 'feil');
            etter(4000, runde);
        }
        else etter(600, runde);
    }

    // =====================================================================
    // Nøkkelscene: nøkkelen kuttes én gang. På «velg passord» følger tennene styrken.
    // =====================================================================
    if (scene && scene.classList.contains('ns')) {
        var steg = +side.getAttribute('data-steg') || 1;
        var nokkel = document.getElementById('nsNokkel');
        var tenner = Array.prototype.slice.call(scene.querySelectorAll('.ns-tann'));
        var visTenner = function (n) { tenner.forEach(function (t, i) { t.classList.toggle('kuttet', i < n); }); };

        if (steg === 3) {
            var p1 = document.getElementById('liPassord1'), p2 = document.getElementById('liPassord2');
            var maler = document.getElementById('liStyrke'), maleTekst = document.getElementById('liStyrkeTekst'), lik = document.getElementById('liLik');
            var oppdater = function () {
                var v = p1 ? p1.value : '';
                var poeng = 0;
                if (v.length >= 8) poeng++;
                if (v.length >= 12) poeng++;
                if (/[0-9]/.test(v) && /[a-zæøå]/i.test(v)) poeng++;
                if (/[A-ZÆØÅ]/.test(v) && /[a-zæøå]/.test(v) || /[^A-Za-z0-9æøåÆØÅ]/.test(v)) poeng++;
                if (v.length > 0 && v.length < 8) poeng = 0;
                visTenner(v.length === 0 ? 0 : Math.max(1, Math.min(6, Math.round(v.length / 14 * 6 * (poeng >= 3 ? 1.3 : 1)))));
                if (maler) maler.setAttribute('data-n', v.length === 0 ? '0' : String(Math.max(1, poeng)));
                if (maleTekst) {
                    maleTekst.className = 'li-hjelp' + (v.length >= 8 ? ' ok' : '');
                    maleTekst.textContent = v.length === 0 ? 'Minst 8 tegn. Tips: en kort setning er lett å huske.'
                        : v.length < 8 ? 'Litt til – ' + (8 - v.length) + ' tegn igjen'
                        : ['Godkjent', 'Godkjent', 'Bra passord', 'Sterkt passord', 'Veldig sterkt passord'][poeng];
                }
                var b = p2 ? p2.value : '';
                var ok = v.length >= 8 && b.length > 0 && v === b;
                if (lik) {
                    lik.className = 'li-hjelp' + (b.length === 0 ? '' : ok ? ' ok' : ' nei');
                    lik.textContent = b.length === 0 ? '' : ok ? '✓ Passordene er like' : 'Passordene er ikke like ennå';
                }
                if (p2) p2.classList.toggle('li-gyldig', ok);
                if (nokkel) nokkel.classList.toggle('sterk', ok);
            };
            if (p1) p1.addEventListener('input', oppdater);
            if (p2) p2.addEventListener('input', oppdater);
            oppdater();
        } else if (rolig) {
            visTenner(6);
        } else {
            tenner.forEach(function (_, i) { etter(700 + i * 350, function () { visTenner(i + 1); }); });
        }
    }
})();
