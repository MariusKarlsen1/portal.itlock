// QR-skanner for driftsportalen (/drift/meld). Bruker nettleserens innebygde
// BarcodeDetector (Chrome/Edge på Android m.fl.). Der den mangler (bl.a. Safari
// på iPhone) svarer start() "ingen-stotte", og siden ber brukeren åpne
// telefonens kamera i stedet - det åpner lenken i QR-merket direkte.
window.driftSkanner = (() => {
    let strom = null;
    let aktiv = false;

    async function start(videoId, dotnet) {
        if (!("BarcodeDetector" in window) || !navigator.mediaDevices?.getUserMedia) {
            return "ingen-stotte";
        }
        let detektor;
        try {
            const formater = await BarcodeDetector.getSupportedFormats();
            if (!formater.includes("qr_code")) return "ingen-stotte";
            detektor = new BarcodeDetector({ formats: ["qr_code"] });
            strom = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" }, audio: false });
        } catch {
            return "ingen-tilgang";
        }

        const video = document.getElementById(videoId);
        if (!video) { stopp(); return "ingen-tilgang"; }
        video.srcObject = strom;
        await video.play();
        aktiv = true;

        const les = async () => {
            if (!aktiv) return;
            try {
                const koder = await detektor.detect(video);
                if (koder.length > 0) {
                    aktiv = false;
                    if (navigator.vibrate) navigator.vibrate(60);
                    await dotnet.invokeMethodAsync("QrLest", koder[0].rawValue);
                    return;
                }
            } catch { /* neste bilde */ }
            setTimeout(les, 250);
        };
        les();
        return "ok";
    }

    function stopp() {
        aktiv = false;
        strom?.getTracks().forEach(t => t.stop());
        strom = null;
    }

    return { start, stopp };
})();
