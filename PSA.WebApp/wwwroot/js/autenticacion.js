document.addEventListener("DOMContentLoaded", function () {
    var overlay = document.getElementById("trobberGlobal");

    var mostrarTrobber = function () {
        if (!overlay) {
            return;
        }

        overlay.classList.remove("d-none");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("trobber-activo");
    };

    var ocultarTrobber = function () {
        if (!overlay) {
            return;
        }

        overlay.classList.add("d-none");
        overlay.setAttribute("aria-hidden", "true");
        document.body.classList.remove("trobber-activo");
    };

    window.psa = window.psa || {};
    window.psa.mostrarTrobberGlobal = mostrarTrobber;
    window.psa.ocultarTrobberGlobal = ocultarTrobber;

    window.addEventListener("pageshow", ocultarTrobber);

    var formularios = [
        document.getElementById("formularioIniciarSesion"),
        document.getElementById("formularioRegistroUsuario"),
        document.getElementById("formularioRecuperarContrasena"),
        document.getElementById("formularioValidarToken"),
        document.getElementById("formularioRestablecerContrasena")
    ].filter(Boolean);

    formularios.forEach(function (formulario) {
        if (window.psa && typeof window.psa.marcarCamposRequeridos === "function") {
            window.psa.marcarCamposRequeridos(formulario);
        }

        formulario.addEventListener("submit", function (evento) {
            var formularioEsValido = formulario.checkValidity();

            if (window.jQuery && window.jQuery.validator && typeof window.jQuery(formulario).valid === "function") {
                formularioEsValido = window.jQuery(formulario).valid();
            }

            if (!formularioEsValido) {
                evento.preventDefault();
                ocultarTrobber();
                return;
            }

            if (formulario.dataset.enviando === "true") {
                evento.preventDefault();
                return;
            }

            formulario.dataset.enviando = "true";

            var boton = formulario.querySelector("[data-loading-button]");
            var texto = formulario.querySelector("[data-loading-texto]");
            var spinner = formulario.querySelector("[data-loading-spinner]");

            if (boton) {
                boton.disabled = true;
                boton.setAttribute("aria-busy", "true");
            }

            if (texto) {
                texto.textContent = "Procesando...";
            }

            if (spinner) {
                spinner.classList.remove("d-none");
            }

            mostrarTrobber();
        });
    });
});
