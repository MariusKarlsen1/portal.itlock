// Lar montøren åpne kameraet og skanne EAN/GTIN-strekkoder på varer som skal
// plukkes på en arbeidsordre, i stedet for å måtte hake av manuelt for hver
// vare - raskere når man står og pakker fysiske varer. Bruker nettleserens
// innebygde BarcodeDetector der den finnes (Chrome/Edge på Android og
// desktop); på nettlesere uten støtte (bl.a. Safari/iOS) vises en tydelig
// feilmelding slik at montøren kan bruke den manuelle avhakingen i stedet.
window.eanSkanner = (function () {
    let stream = null;
    let detector = null;
    let skannerLoop = null;

    function stotter() {
        return 'BarcodeDetector' in window;
    }

    async function start(videoEl, dotNetRef) {
        if (!stotter()) {
            await dotNetRef.invokeMethodAsync('OnSkannerFeil', 'Denne nettleseren støtter ikke strekkodeskanning. Bruk avhakingen manuelt i stedet.');
            return;
        }

        try {
            detector = new BarcodeDetector({ formats: ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128'] });
            stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
            videoEl.srcObject = stream;
            await videoEl.play();
        } catch (err) {
            await dotNetRef.invokeMethodAsync('OnSkannerFeil', 'Fikk ikke tilgang til kamera: ' + err.message);
            return;
        }

        let sisteKode = null;
        let sisteTid = 0;

        const lesAvRamme = async () => {
            if (!stream) {
                return;
            }
            try {
                const koder = await detector.detect(videoEl);
                if (koder.length > 0) {
                    const kode = koder[0].rawValue;
                    const naa = Date.now();
                    // Hindrer at samme strekkode meldes flere ganger i sekundet
                    // mens den fortsatt er i bildet.
                    if (kode !== sisteKode || naa - sisteTid > 1500) {
                        sisteKode = kode;
                        sisteTid = naa;
                        await dotNetRef.invokeMethodAsync('OnStrekkodeSkannet', kode);
                    }
                }
            } catch {
                // Ignorerer enkeltbilder som feiler å analysere - prøver igjen på neste ramme.
            }
            skannerLoop = requestAnimationFrame(lesAvRamme);
        };
        skannerLoop = requestAnimationFrame(lesAvRamme);
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
    }

    return { start, stop, stotter };
})();
