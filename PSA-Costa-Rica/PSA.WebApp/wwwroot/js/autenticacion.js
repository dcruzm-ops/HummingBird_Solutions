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
    });
});