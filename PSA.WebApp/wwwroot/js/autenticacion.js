document.addEventListener("DOMContentLoaded", function () {
    var formularios = [
        document.getElementById("formularioIniciarSesion"),
        document.getElementById("formularioRegistroUsuario"),
        document.getElementById("formularioRecuperarContrasena"),
        document.getElementById("formularioRestablecerContrasena")
    ].filter(Boolean);

    formularios.forEach(function (formulario) {
        if (window.psa && typeof window.psa.marcarCamposRequeridos === "function") {
            window.psa.marcarCamposRequeridos(formulario);
        }

        formulario.addEventListener("submit", function () {
            var boton = formulario.querySelector("[data-loading-button]");
            var texto = formulario.querySelector("[data-loading-texto]");
            var spinner = formulario.querySelector("[data-loading-spinner]");

            if (!boton) {
                return;
            }

            boton.disabled = true;
            boton.setAttribute("aria-busy", "true");

            if (texto) {
                texto.textContent = "Procesando...";
            }

            if (spinner) {
                spinner.classList.remove("d-none");
            }
        });
    });
});
