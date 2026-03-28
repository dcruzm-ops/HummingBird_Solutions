(function () {
    window.psa = window.psa || {};

    window.psa.mostrarMensajeConsola = function (mensaje) {
        console.log("[PSA Costa Rica] " + mensaje);
    };

    window.psa.serializarFormulario = function (formulario) {
        var datos = {};
        if (!formulario) {
            return datos;
        }

        new FormData(formulario).forEach(function (valor, llave) {
            datos[llave] = valor;
        });

        return datos;
    };

    window.psa.marcarCamposRequeridos = function (formulario) {
        if (!formulario) {
            return;
        }

        formulario.querySelectorAll("input, select, textarea").forEach(function (campo) {
            campo.addEventListener("blur", function () {
                if (campo.hasAttribute("required") && !campo.value.trim()) {
                    campo.setAttribute("aria-invalid", "true");
                } else {
                    campo.removeAttribute("aria-invalid");
                }
            });
        });
    };
})();
