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

    window.psa.normalizarTextosInterfaz = function () {
        var reemplazos = {
            "NombreCompleto": "Nombre Completo",
            "FechaNacimiento": "Fecha de Nacimiento",
            "TipoVegetacion": "Tipo de Vegetación",
            "EstadoEvaluacion": "Estado de Evaluación",
            "DecisionTecnica": "Decisión Técnica",
            "CuentaBancaria": "Cuenta Bancaria",
            "HistorialPagos": "Historial de Pagos",
            "PendienteDatosBancarios": "Pendiente de Datos Bancarios",
            "PendienteAprobacionFinal": "Pendiente de Aprobación Final",
            "NoCalifica": "No Califica"
        };

        var elementos = document.querySelectorAll('label, th, option, .badge-estado, .texto-normalizable');

        elementos.forEach(function (el) {
            var texto = (el.textContent || '').trim();
            if (!texto) {
                return;
            }

            if (reemplazos[texto]) {
                el.textContent = reemplazos[texto];
                return;
            }

            if (/^[A-Z][a-z]+([A-Z][a-z0-9]+)+$/.test(texto) || /^[a-z]+([A-Z][a-z0-9]+)+$/.test(texto)) {
                el.textContent = texto.replace(/([a-z\d])([A-Z])/g, '$1 $2');
            }
        });
    };

    document.addEventListener('DOMContentLoaded', function () {
        window.psa.normalizarTextosInterfaz();
    });
})();
