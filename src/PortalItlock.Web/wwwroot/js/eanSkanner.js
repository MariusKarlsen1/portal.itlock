// Lar montøren åpne kameraet og skanne EAN/GTIN-strekkoder på varer som skal
// plukkes på en arbeidsordre, i stedet for å måtte hake av manuelt for hver
// vare - raskere når man står og pakker fysiske varer. Bruker nettleserens
// innebygde BarcodeDetector der den finnes (Chrome/Edge på Android og
// desktop). Mangler den (bl.a. Safari på iPhone og Firefox), lastes
// html5-qrcode (ZXing) inn første gang og leser strekkoden fra kameraet i
// stedet, så skanningen virker i alle nettlesere.
window.eanSkanner = (function () {
    const BIBLIOTEK = 'https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js';

    let stream = null;
    let detector = null;
    let skannerLoop = null;
    let reserve = null;
    let reserveHolder = null;

    function stotter() {
        return true;
    }

    function harInnebygd() {
        return 'BarcodeDetector' in window;
    }

    function lastBibliotek() {
        if (window.Html5Qrcode) {
            return Promise.resolve();
        }
        return new Promise((ok, feil) => {
            const s = document.createElement('script');
            s.src = BIBLIOTEK;
            s.onload = () => ok();
            s.onerror = () => feil(new Error('Kunne ikke laste skanneren. Sjekk nettforbindelsen.'));
            document.head.appendChild(s);
        });
    }

    // Samme strekkode meldes ikke flere ganger i sekundet mens den er i bildet.
    function lagMelder(dotNetRef) {
        let sisteKode = null;
        let sisteTid = 0;
        return async kode => {
            const naa = Date.now();
            if (kode !== sisteKode || naa - sisteTid > 1500) {
                sisteKode = kode;
                sisteTid = naa;
                if (navigator.vibrate) navigator.vibrate(60);
                await dotNetRef.invokeMethodAsync('OnStrekkodeSkannet', kode);
            }
        };
    }

    async function start(videoEl, dotNetRef) {
        stop();
        const meld = lagMelder(dotNetRef);

        if (harInnebygd()) {
            try {
                detector = new BarcodeDetector({ formats: ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128'] });
                stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
                videoEl.srcObject = stream;
                await videoEl.play();
            } catch (err) {
                await dotNetRef.invokeMethodAsync('OnSkannerFeil', 'Fikk ikke tilgang til kamera: ' + err.message);
                return;
            }

            const lesAvRamme = async () => {
                if (!stream) {
                    return;
                }
                try {
                    const koder = await detector.detect(videoEl);
                    if (koder.length > 0) {
                        await meld(koder[0].rawValue);
                    }
                } catch {
                    // Ignorerer enkeltbilder som feiler å analysere - prøver igjen på neste ramme.
                }
                skannerLoop = requestAnimationFrame(lesAvRamme);
            };
            skannerLoop = requestAnimationFrame(lesAvRamme);
            return;
        }

        // Reserve: html5-qrcode tegner sin egen video i en egen boks foran
        // video-elementet, som skjules så lenge.
        try {
            await lastBibliotek();
        } catch (err) {
            await dotNetRef.invokeMethodAsync('OnSkannerFeil', err.message);
            return;
        }

        reserveHolder = document.createElement('div');
        reserveHolder.id = 'ean-skanner-' + Date.now();
        reserveHolder.style.width = '100%';
        videoEl.style.display = 'none';
        videoEl.parentElement.insertBefore(reserveHolder, videoEl);

        const F = window.Html5QrcodeSupportedFormats;
        reserve = new window.Html5Qrcode(reserveHolder.id, {
            formatsToSupport: [F.EAN_13, F.EAN_8, F.UPC_A, F.UPC_E, F.CODE_128],
            verbose: false
        });
        try {
            await reserve.start(
                { facingMode: 'environment' },
                { fps: 10, qrbox: (b, h) => ({ width: Math.floor(b * 0.85), height: Math.floor(h * 0.45) }) },
                kode => { meld(kode); },
                () => { });
        } catch (err) {
            await dotNetRef.invokeMethodAsync('OnSkannerFeil', 'Fikk ikke tilgang til kamera: ' + (err && err.message ? err.message : err));
        }
    }

    function stop() {
        if (skannerLoop) {
            cancelAnimationFrame(skannerLoop);
            skannerLoop = null;
        }
        if (stream) {
            stream.getTracks().forEach(t => t.stop());
            stream = null;
        }
        detector = null;

        if (reserve) {
            const r = reserve;
            const holder = reserveHolder;
            reserve = null;
            reserveHolder = null;
            r.stop().catch(() => { }).finally(() => {
                try { r.clear(); } catch { }
                if (holder) {
                    const video = holder.nextElementSibling;
                    if (video && video.tagName === 'VIDEO') video.style.display = '';
                    holder.remove();
                }
            });
        }
    }

    return { start, stop, stotter };
})();
