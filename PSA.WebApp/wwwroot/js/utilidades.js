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

    window.psa.obtenerDuracionToast = function (tipo) {
        switch ((tipo || "").toLowerCase()) {
            case "exito":
                return 3200;
            case "info":
                return 5000;
            case "advertencia":
                return 7000;
            case "error":
                return 10000;
            default:
                return 5000;
        }
    };

    window.psa.inicializarToasts = function () {
        document.querySelectorAll(".toast-sistema").forEach(function (toast) {
            var tipo = toast.getAttribute("data-toast-tipo") || "info";
            var autoDismiss = tipo !== "error";
            var duracion = window.psa.obtenerDuracionToast(tipo);

            var cerrarToast = function () {
                if (!toast || !toast.parentElement) return;
                toast.classList.add("toast-sistema--cerrando");
                window.setTimeout(function () {
                    toast.remove();
                }, 220);
            };

            var botonCerrar = toast.querySelector("[data-toast-cerrar]");
            if (botonCerrar) {
                botonCerrar.addEventListener("click", cerrarToast);
            }

            if (autoDismiss) {
                window.setTimeout(cerrarToast, duracion);
            }
        });
    };

    window.psa.inicializarConfirmaciones = function () {
        var modalElement = document.getElementById("modalConfirmacionSistema");
        if (!modalElement || typeof bootstrap === "undefined") return;

        var modal = bootstrap.Modal.getOrCreateInstance(modalElement);
        var titulo = document.getElementById("modalConfirmacionSistemaTitulo");
        var mensaje = document.getElementById("modalConfirmacionSistemaMensaje");
        var aceptar = document.getElementById("modalConfirmacionSistemaAceptar");
        var accionPendiente = null;

        var abrirModal = function (config) {
            if (titulo) titulo.textContent = config.titulo || "Confirmar acción";
            if (mensaje) mensaje.textContent = config.mensaje || "¿Desea continuar con esta acción?";
            accionPendiente = config.onConfirm || null;
            modal.show();
        };

        document.querySelectorAll("form, a[data-confirm-message]").forEach(function (elemento) {
            var mensajeConfirmacion = elemento.getAttribute("data-confirm-message");
            if (mensajeConfirmacion) {
                elemento.addEventListener("click", function (evento) {
                    if (elemento.dataset.confirmado === "true") {
                        elemento.dataset.confirmado = "false";
                        return;
                    }
                    evento.preventDefault();
                    abrirModal({
                        titulo: elemento.getAttribute("data-confirm-title") || "Confirmar acción",
                        mensaje: mensajeConfirmacion,
                        onConfirm: function () {
                            elemento.dataset.confirmado = "true";
                            elemento.click();
                        }
                    });
                });
                return;
            }

            if (elemento.tagName !== "FORM") return;
            if (elemento.closest(".modal")) return;
            var accion = (elemento.getAttribute("action") || "").toLowerCase();
            var requiereConfirmacion = /(eliminar|inactivar|aprobar|rechazar|validar|cerrarsesion)/.test(accion);
            if (!requiereConfirmacion || elemento.hasAttribute("data-confirm-skip")) return;

            elemento.addEventListener("submit", function (evento) {
                if (elemento.dataset.confirmado === "true") {
                    elemento.dataset.confirmado = "false";
                    return;
                }
                evento.preventDefault();
                abrirModal({
                    titulo: "Confirmar acción",
                    mensaje: "Esta acción puede cambiar el estado del sistema. ¿Desea continuar?",
                    onConfirm: function () {
                        elemento.dataset.confirmado = "true";
                        elemento.requestSubmit();
                    }
                });
            });
        });

        if (aceptar) {
            aceptar.addEventListener("click", function () {
                if (typeof accionPendiente === "function") {
                    accionPendiente();
                }
                accionPendiente = null;
                modal.hide();
            });
        }
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
        window.psa.inicializarToasts();
        window.psa.inicializarConfirmaciones();
    });
})();
