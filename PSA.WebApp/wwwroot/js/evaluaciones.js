document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioNuevaEvaluacion");
    if (!formulario) {
        return;
    }

    var inputFechaVisita = formulario.querySelector("input[name='Formulario.FechaVisita']");
    var camposAjuste = formulario.querySelectorAll(".ajuste-campo");

    function toggleCamposAjuste() {
        var habilitar = !!(inputFechaVisita && inputFechaVisita.value);
        camposAjuste.forEach(function (campo) {
            campo.disabled = !habilitar;
        });
    }

    if (inputFechaVisita) {
        inputFechaVisita.addEventListener("change", toggleCamposAjuste);
        toggleCamposAjuste();
    }

    formulario.addEventListener("submit", function () {
        var boton = formulario.querySelector("button[type='submit']");
        if (boton) {
            boton.disabled = true;
            boton.textContent = "Guardando...";
        }
    });
});