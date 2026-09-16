// Lar brukeren klikke på Bilde-feltet på varekortet og lime inn (Ctrl+V) et
// bilde rett fra utklippstavlen, i stedet for å måtte trykke "Velg fil" og
// bla frem en fil på disk hver gang - mye raskere når bildet allerede ligger
// på utklippstavlen (f.eks. kopiert fra en leverandørs nettbutikk).
window.vareBilde = (function () {
    function kobleLim(containerId, dotNetRef) {
        const container = document.getElementById(containerId);
        if (!container || container.dataset.limKoblet) {
            return;
        }
        container.dataset.limKoblet = "1";

        container.addEventListener('paste', e => {
            const items = e.clipboardData && e.clipboardData.items;
            if (!items) {
                return;
            }

            const bildeItem = [...items].find(item => item.type.startsWith('image/'));
            if (!bildeItem) {
                return;
            }

            e.preventDefault();
            const blob = bildeItem.getAsFile();
            const reader = new FileReader();
            reader.onload = () => {
                const base64 = reader.result.split(',')[1];
                dotNetRef.invokeMethodAsync('MottaLimtInnBilde', base64, blob.type);
            };
            reader.readAsDataURL(blob);
        });
    }

    return { kobleLim };
})();
