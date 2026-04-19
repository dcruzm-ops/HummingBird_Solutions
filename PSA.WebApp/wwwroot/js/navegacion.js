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
        var menuColapsadoGuardado = localStorage.getItem("psa-menu-colapsado");
        if (menuColapsadoGuardado === "true") {
            document.body.classList.add("menu-colapsado");
        }

        botonMenuLateral.addEventListener("click", function () {
            var esPantallaMovil = window.matchMedia("(max-width: 992px)").matches;
            if (esPantallaMovil) {
                barraLateral.classList.toggle("abierta");
                return;
            }

            document.body.classList.toggle("menu-colapsado");
            localStorage.setItem("psa-menu-colapsado", document.body.classList.contains("menu-colapsado") ? "true" : "false");
        });
    }

    var overlay = document.getElementById("trobberGlobal");
    var mostrarTrobber = function () {
        if (!overlay) {
            return;
        }

        overlay.classList.remove("d-none");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("trobber-activo");
    };

    var ocultarTrobber = function () {
        if (!overlay) {
            return;
        }

        overlay.classList.add("d-none");
        overlay.setAttribute("aria-hidden", "true");
        document.body.classList.remove("trobber-activo");
    };

    window.psa = window.psa || {};
    window.psa.mostrarTrobberGlobal = mostrarTrobber;
    window.psa.ocultarTrobberGlobal = ocultarTrobber;

    var alertasAutoDismiss = document.querySelectorAll("[data-auto-dismiss-ms]");
    alertasAutoDismiss.forEach(function (alerta) {
        var tiempo = Number(alerta.getAttribute("data-auto-dismiss-ms")) || 8000;
        window.setTimeout(function () {
            alerta.classList.add("alerta-desvanecer");
            window.setTimeout(function () {
                alerta.remove();
            }, 550);
        }, tiempo);
    });

    document.querySelectorAll("a[href]").forEach(function (enlace) {
        enlace.addEventListener("click", function (evento) {
            if (enlace.getAttribute("aria-disabled") === "true") {
                evento.preventDefault();
                return;
            }

            if (evento.defaultPrevented || !enlace.href) {
                return;
            }

            var href = enlace.getAttribute("href") || "";
            if (href.startsWith("#") || enlace.target === "_blank" || enlace.hasAttribute("download")) {
                return;
            }

            if (enlace.origin !== window.location.origin) {
                return;
            }

            mostrarTrobber();
        });
    });

    document.querySelectorAll("form").forEach(function (formulario) {
        formulario.addEventListener("submit", function () {
            if (formulario.hasAttribute("data-omitir-trobber-global")) {
                return;
            }

            if (formulario.checkValidity()) {
                mostrarTrobber();
            }
        });
    });

    window.addEventListener("pageshow", ocultarTrobber);
});
// usar para menu de navegacion  lateral
