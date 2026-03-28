document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioRegistrarFinca");
    if (!formulario) {
        return;
    }

    formulario.addEventListener("submit", function (evento) {
        evento.preventDefault();
        var datos = window.psa ? window.psa.serializarFormulario(formulario) : {};
        console.log("Formulario de finca listo para integrar con API:", datos);
        alert("Base del formulario de finca creada. Falta conectar la persistencia.");
    });
});
