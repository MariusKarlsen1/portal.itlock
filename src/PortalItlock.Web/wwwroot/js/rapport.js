// Hjelpere for rapportene under /rapporter: last ned tabell som CSV
// (åpnes i Excel) og skriv ut / lagre som PDF via nettleseren.
window.rapport = {
    lastNedCsv: function (filnavn, innhold) {
        // BOM slik at Excel leser æ/ø/å riktig.
        const blob = new Blob(["﻿" + innhold], { type: "text/csv;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filnavn;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },
    skrivUt: function () {
        window.print();
    }
};
