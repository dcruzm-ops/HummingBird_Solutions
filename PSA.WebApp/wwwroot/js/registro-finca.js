document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioRegistrarFinca");
    if (!formulario) {
        return;
    }

    formulario.addEventListener("submit", function (evento) {
        if (!formulario.checkValidity()) {
            evento.preventDefault();
            evento.stopPropagation();
            formulario.classList.add("was-validated");
        }
    });
});
