// Piccole funzioni JS richiamate da Blazor
window.grimorio = {
    scrollTop: function () { window.scrollTo({ top: 0, behavior: "instant" }); },
    // Centra la vista della griglia di battaglia su una cella (token selezionato / turno corrente)
    centerOn: function (containerId, x, y) {
        const el = document.getElementById(containerId);
        if (!el) return;
        el.scrollTo({ left: Math.max(0, x - el.clientWidth / 2), top: Math.max(0, y - el.clientHeight / 2), behavior: "smooth" });
    }
};
