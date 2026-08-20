window.signatur = (function () {
    const pads = {};

    function init(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const ctx = canvas.getContext('2d');
        ctx.lineWidth = 2.5;
        ctx.lineCap = 'round';
        ctx.strokeStyle = '#1a1a1a';

        let tegner = false;
        let harTegnet = false;

        function posisjon(e) {
            const rect = canvas.getBoundingClientRect();
            return {
                x: (e.clientX - rect.left) * (canvas.width / rect.width),
                y: (e.clientY - rect.top) * (canvas.height / rect.height)
            };
        }

        function start(e) {
            tegner = true;
            const p = posisjon(e);
            ctx.beginPath();
            ctx.moveTo(p.x, p.y);
            e.preventDefault();
        }

        function tegn(e) {
            if (!tegner) {
                return;
            }
            const p = posisjon(e);
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
            harTegnet = true;
            e.preventDefault();
        }

        function slutt() {
            tegner = false;
        }

        canvas.addEventListener('pointerdown', start);
        canvas.addEventListener('pointermove', tegn);
        canvas.addEventListener('pointerup', slutt);
        canvas.addEventListener('pointerleave', slutt);

        pads[canvasId] = { canvas, ctx, erTom: () => !harTegnet, nullstill: () => { harTegnet = false; } };
    }

    function clear(canvasId) {
        const pad = pads[canvasId];
        if (!pad) {
            return;
        }
        pad.ctx.clearRect(0, 0, pad.canvas.width, pad.canvas.height);
        pad.nullstill();
    }

    function isEmpty(canvasId) {
        const pad = pads[canvasId];
        return !pad || pad.erTom();
    }

    function getPngBase64(canvasId) {
        const pad = pads[canvasId];
        if (!pad) {
            return null;
        }
        return pad.canvas.toDataURL('image/png').split(',')[1];
    }

    return { init, clear, isEmpty, getPngBase64 };
})();
