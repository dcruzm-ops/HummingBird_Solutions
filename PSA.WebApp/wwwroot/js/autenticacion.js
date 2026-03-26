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

        formulario.addEventListener("submit", function (evento) {
            evento.preventDefault();
            var datos = window.psa ? window.psa.serializarFormulario(formulario) : {};
            console.log("Formulario de autenticación listo para integrar con API:", datos);
            alert("Base visual lista. Falta integrar este formulario con WebAPI.");
        });
    });
});
