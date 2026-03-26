document.addEventListener("DOMContentLoaded", function () {
    var botonTema = document.getElementById("botonTema");
    var botonMenuLateral = document.getElementById("botonMenuLateral");
    var barraLateral = document.getElementById("barraLateral");
    var raizHtml = document.documentElement;

    if (botonTema) {
        botonTema.addEventListener("click", function () {
            var temaActual = raizHtml.getAttribute("data-tema") || "claro";
            var nuevoTema = temaActual === "claro" ? "oscuro" : "claro";
            raizHtml.setAttribute("data-tema", nuevoTema);
            localStorage.setItem("psa-tema", nuevoTema);
        });
    }

    var temaGuardado = localStorage.getItem("psa-tema");
    if (temaGuardado) {
        raizHtml.setAttribute("data-tema", temaGuardado);
    }

    if (botonMenuLateral && barraLateral) {
        botonMenuLateral.addEventListener("click", function () {
            barraLateral.classList.toggle("abierta");
        });
    }
});
// usar para menu de navegacion  lateral