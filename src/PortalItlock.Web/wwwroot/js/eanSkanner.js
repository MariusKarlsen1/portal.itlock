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
    let aktivRef = null;

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

    // Forklarer hvorfor kameraet ikke startet, så montøren vet hva som må gjøres.
    function kameraFeil(err) {
        const navn = err && err.name ? err.name : '';
        const tekst = String(err && err.message ? err.message : err || '');
        if (!window.isSecureContext || !navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            return 'Kameraet er blokkert: siden må åpnes over en sikker tilkobling (https).';
        }
        if (navn === 'NotAllowedError' || navn === 'SecurityError' || /notallowed|permission|denied|not allowed/i.test(tekst)) {
            return 'Kameraet er blokkert for denne siden. Slå på kameratilgang i telefonens innstillinger, eller ta bilde av strekkoden.';
        }
        if (navn === 'NotFoundError' || navn === 'OverconstrainedError' || /notfound|no camera|requested device not found/i.test(tekst)) {
            return 'Fant ikke noe kamera på enheten. Ta bilde av strekkoden eller skriv inn varenummeret.';
        }
        if (navn === 'NotReadableError' || navn === 'AbortError' || /notreadable|could not start|in use/i.test(tekst)) {
            return 'Kameraet brukes av en annen app. Lukk den og prøv igjen.';
        }
        return 'Fikk ikke startet kameraet (' + (navn || tekst) + '). Ta bilde av strekkoden eller skriv inn varenummeret.';
    }

    async function start(videoEl, dotNetRef) {
        stop();
        aktivRef = dotNetRef;
        const meld = lagMelder(dotNetRef);

        if (harInnebygd()) {
            try {
                detector = new BarcodeDetector({ formats: ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128'] });
                stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
                videoEl.srcObject = stream;
                await videoEl.play();
            } catch (err) {
                stop();
                await dotNetRef.invokeMethodAsync('OnSkannerFeil', kameraFeil(err));
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
            await dotNetRef.invokeMethodAsync('OnSkannerFeil', kameraFeil(err));
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

    // Leser strekkoden fra et bilde tatt med telefonens vanlige kamera. Det
    // trenger ingen kameratillatelse i nettleseren, så det virker også når
    // kameraet er blokkert for siden.
    async function lesBilde(input) {
        const fil = input && input.files && input.files[0];
        const ref = aktivRef;
        if (input) input.value = '';
        if (!fil || !ref) {
            return;
        }

        let kode = null;
        try {
            if (harInnebygd()) {
                const bilde = await createImageBitmap(fil);
                const funn = await new BarcodeDetector({ formats: ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128'] }).detect(bilde);
                kode = funn.length > 0 ? funn[0].rawValue : null;
            }
            if (!kode) {
                await lastBibliotek();
                let holder = document.getElementById('ean-bilde-holder');
                if (!holder) {
                    holder = document.createElement('div');
                    holder.id = 'ean-bilde-holder';
                    holder.style.display = 'none';
                    document.body.appendChild(holder);
                }
                const F = window.Html5QrcodeSupportedFormats;
                const leser = new window.Html5Qrcode(holder.id, { formatsToSupport: [F.EAN_13, F.EAN_8, F.UPC_A, F.UPC_E, F.CODE_128], verbose: false });
                try {
                    kode = await leser.scanFile(fil, false);
                } catch {
                    kode = null;
                } finally {
                    try { leser.clear(); } catch { }
                }
            }
        } catch {
            kode = null;
        }

        if (kode) {
            await ref.invokeMethodAsync('OnStrekkodeSkannet', kode);
        } else {
            await ref.invokeMethodAsync('OnSkannerFeil', 'Fant ingen strekkode i bildet. Ta bildet nærmere, rett forfra og med godt lys.');
        }
    }

    return { start, stop, stotter, lesBilde };
})();
