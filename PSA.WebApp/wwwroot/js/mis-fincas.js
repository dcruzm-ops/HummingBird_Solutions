document.addEventListener("DOMContentLoaded", function () {
    var campoBusqueda = document.getElementById("buscarFinca");
    var filtroEstado = document.getElementById("estadoFiltro");
    var filtroProvincia = document.getElementById("provinciaFiltro");
    var filas = Array.from(document.querySelectorAll(".tabla-base tbody tr[data-nombre]"));

    if (filas.length === 0) {
        return;
    }

    function normalizar(valor) {
        return (valor || "")
            .toString()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .trim();
    }

    function aplicarFiltros() {
        var textoBusqueda = normalizar(campoBusqueda ? campoBusqueda.value : "");
        var estadoSeleccionado = normalizar(filtroEstado ? filtroEstado.value : "");
        var provinciaSeleccionada = normalizar(filtroProvincia ? filtroProvincia.value : "");

        filas.forEach(function (fila) {
            var nombre = normalizar(fila.dataset.nombre);
            var expediente = normalizar(fila.dataset.expediente);
            var provincia = normalizar(fila.dataset.provincia);
            var canton = normalizar(fila.dataset.canton);
            var estado = normalizar(fila.dataset.estado);
            var evaluacion = normalizar(fila.dataset.evaluacion);

            var coincideBusqueda = !textoBusqueda
                || nombre.includes(textoBusqueda)
                || expediente.includes(textoBusqueda)
                || provincia.includes(textoBusqueda)
                || canton.includes(textoBusqueda)
                || estado.includes(textoBusqueda)
                || evaluacion.includes(textoBusqueda);

            var coincideEstado = !estadoSeleccionado || estado.includes(estadoSeleccionado);
            var coincideProvincia = !provinciaSeleccionada || provincia === provinciaSeleccionada;

            fila.classList.toggle("d-none", !(coincideBusqueda && coincideEstado && coincideProvincia));
        });
    }

    if (campoBusqueda) {
        campoBusqueda.addEventListener("input", aplicarFiltros);
    }

    if (filtroEstado) {
        filtroEstado.addEventListener("change", aplicarFiltros);
    }

    if (filtroProvincia) {
        filtroProvincia.addEventListener("change", aplicarFiltros);
    }
});
