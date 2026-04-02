document.addEventListener("DOMContentLoaded", function () {
    var mapaElemento = document.getElementById("mapaDetalleFinca");
    if (!mapaElemento || typeof window.L === "undefined") {
        return;
    }

    var latitudTexto = (mapaElemento.dataset.latitud || "").replace(",", ".").trim();
    var longitudTexto = (mapaElemento.dataset.longitud || "").replace(",", ".").trim();
    var latitud = parseFloat(latitudTexto);
    var longitud = parseFloat(longitudTexto);

    if (!Number.isFinite(latitud) || !Number.isFinite(longitud) || (latitud === 0 && longitud === 0)) {
        mapaElemento.innerHTML = "<p class='texto-ayuda p-3'>No hay coordenadas válidas para mostrar el mapa.</p>";
        return;
    }

    var nombreFinca = mapaElemento.dataset.nombreFinca || "Finca";
    var mapa = window.L.map("mapaDetalleFinca", {
        center: [latitud, longitud],
        zoom: 14,
        scrollWheelZoom: false
    });

    window.L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; OpenStreetMap contributors"
    }).addTo(mapa);

    window.L.marker([latitud, longitud])
        .addTo(mapa)
        .bindPopup(nombreFinca + "<br/>Lat: " + latitud.toFixed(6) + " / Lon: " + longitud.toFixed(6))
        .openPopup();

    setTimeout(function () {
        mapa.invalidateSize();
    }, 100);
});
