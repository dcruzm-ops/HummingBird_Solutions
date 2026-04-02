document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioNuevaEvaluacion");
    if (!formulario) {
        return;
    }

    formulario.addEventListener("submit", function () {
        var boton = formulario.querySelector("button[type='submit']");
        if (boton) {
            boton.disabled = true;
            boton.textContent = "Guardando...";
        }
    });
});
