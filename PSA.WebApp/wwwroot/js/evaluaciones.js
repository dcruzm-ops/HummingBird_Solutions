document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioNuevaEvaluacion");
    if (!formulario) {
        return;
    }

    formulario.addEventListener("submit", function (evento) {
        evento.preventDefault();
        var datos = window.psa ? window.psa.serializarFormulario(formulario) : {};
        console.log("Formulario de evaluación listo para integrar con API:", datos);
        alert("Base de evaluación creada. Integre este formulario con la WebAPI.");
    });
});
