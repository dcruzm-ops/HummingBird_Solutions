document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioRegistrarFinca");
    if (!formulario) {
        return;
    }

    formulario.addEventListener("submit", function (evento) {
        var campoHectareas = document.getElementById("hectareas");
        if (!campoHectareas) {
            return;
        }

        var valor = Number(campoHectareas.value);
        if (!Number.isFinite(valor) || valor <= 0) {
            evento.preventDefault();
            alert("Las hectáreas deben ser un número positivo mayor a 0.");
            campoHectareas.focus();
        }
    });
});
