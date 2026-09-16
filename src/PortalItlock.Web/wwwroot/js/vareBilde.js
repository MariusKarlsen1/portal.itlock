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

    // Lar brukeren dra en fil fra egen PC og slippe den rett på
    // FDV/Monteringsanvisning/Datablad-feltet, som et raskere alternativ til
    // å trykke "Velg fil" og bla frem filen i en dialog. Gjenbruker Blazor
    // sin egen InputFile-opplasting ved å legge den sluppede filen inn i det
    // skjulte <input type="file"> og trigge et vanlig change-event - da går
    // filen gjennom nøyaktig samme strømmede serveropplasting som ved klikk,
    // uten noen størrelsesbegrensning fra SignalR.
    function kobleDrop(containerId, inputId) {
        const container = document.getElementById(containerId);
        const input = document.getElementById(inputId);
        if (!container || !input || container.dataset.dropKoblet) {
            return;
        }
        container.dataset.dropKoblet = "1";

        container.addEventListener('drop', e => {
            const files = e.dataTransfer && e.dataTransfer.files;
            if (!files || files.length === 0) {
                return;
            }
            e.preventDefault();
            const dt = new DataTransfer();
            dt.items.add(files[0]);
            input.files = dt.files;
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
    }

    return { kobleLim, kobleDrop };
})();
