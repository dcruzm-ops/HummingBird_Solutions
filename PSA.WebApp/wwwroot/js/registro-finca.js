document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioRegistrarFinca");
    if (!formulario) {
        return;
    }

    var boton = formulario.querySelector("[data-loading-button]");
    var textoBoton = formulario.querySelector("[data-loading-texto]");
    var spinnerBoton = formulario.querySelector("[data-loading-spinner]");

    var provinciaInput = document.getElementById("provinciaInput");
    var cantonInput = document.getElementById("cantonInput");
    var distritoInput = document.getElementById("distritoInput");
    var latitudInput = document.getElementById("Latitud");
    var longitudInput = document.getElementById("Longitud");
    var tieneRiosOQuebradasCheck = document.getElementById("tieneRiosOQuebradas");
    var tieneNacientesCheck = document.getElementById("tieneNacientes");
    var cantidadNacientesInput = document.getElementById("cantidadNacientes");
    var tieneRecursosHidricosInput = document.getElementById("tieneRecursosHidricos");

    function obtenerMensajeValidacion(campo) {
        var nombreCampo = (campo.labels && campo.labels[0] && campo.labels[0].textContent)
            ? campo.labels[0].textContent.trim()
            : "Este campo";

        if (campo.validity.valueMissing) {
            return "El campo '" + nombreCampo + "' es obligatorio.";
        }

        if (campo.validity.typeMismatch) {
            return "El valor de '" + nombreCampo + "' no es válido.";
        }

        if (campo.validity.rangeUnderflow || campo.validity.rangeOverflow) {
            return "El valor de '" + nombreCampo + "' está fuera del rango permitido.";
        }

        if (campo.validity.stepMismatch) {
            return "El formato numérico de '" + nombreCampo + "' no es válido.";
        }

        return "";
    }

    function configurarMensajesValidacionEspanol() {
        formulario.querySelectorAll("input, select, textarea").forEach(function (campo) {
            campo.addEventListener("invalid", function () {
                campo.setCustomValidity(obtenerMensajeValidacion(campo));
            });

            campo.addEventListener("input", function () {
                campo.setCustomValidity("");
            });

            campo.addEventListener("change", function () {
                campo.setCustomValidity("");
            });
        });
    }

    function sincronizarRecursosHidricos() {
        if (!tieneRecursosHidricosInput) {
            return;
        }

        var tieneRios = !!(tieneRiosOQuebradasCheck && tieneRiosOQuebradasCheck.checked);
        var tieneNacientes = !!(tieneNacientesCheck && tieneNacientesCheck.checked);
        var hayRecursos = tieneRios || tieneNacientes;

        tieneRecursosHidricosInput.value = hayRecursos ? "true" : "false";

        if (cantidadNacientesInput) {
            cantidadNacientesInput.disabled = !tieneNacientes;
            if (!tieneNacientes) {
                cantidadNacientesInput.value = "0";
            } else if (!cantidadNacientesInput.value) {
                cantidadNacientesInput.value = "1";
            }
        }
    }

    var mapaContenedor = document.getElementById("mapaUbicacionFinca");
    var mapa = null;
    var marcador = null;

    function setCoordenadas(latitud, longitud) {
        if (latitudInput) {
            latitudInput.value = Number(latitud).toFixed(7);
        }
        if (longitudInput) {
            longitudInput.value = Number(longitud).toFixed(7);
        }
    }

    function setUbicacionAdministrativa(provincia, canton, distrito) {
        if (provinciaInput) {
            provinciaInput.value = provincia || "";
        }
        if (cantonInput) {
            cantonInput.value = canton || "";
        }
        if (distritoInput) {
            distritoInput.value = distrito || "";
        }
    }

    function extraerPrimerValorValido() {
        for (var i = 0; i < arguments.length; i++) {
            if (arguments[i] && String(arguments[i]).trim()) {
                return String(arguments[i]).trim();
            }
        }
        return "";
    }

    function resolverUbicacionAdministrativa(latitud, longitud) {
        var url = "https://nominatim.openstreetmap.org/reverse?format=jsonv2&accept-language=es&zoom=18&lat="
            + encodeURIComponent(latitud)
            + "&lon="
            + encodeURIComponent(longitud);

        fetch(url, {
            headers: {
                "Accept": "application/json"
            }
        })
            .then(function (respuesta) {
                if (!respuesta.ok) {
                    throw new Error("No se pudo resolver la ubicación administrativa");
                }
                return respuesta.json();
            })
            .then(function (resultado) {
                var address = resultado && resultado.address ? resultado.address : {};

                var provincia = extraerPrimerValorValido(address.state, address.region, address.province);
                var canton = extraerPrimerValorValido(address.county, address.city, address.town, address.municipality);
                var distrito = extraerPrimerValorValido(address.city_district, address.suburb, address.village, address.hamlet, address.neighbourhood);

                setUbicacionAdministrativa(provincia, canton, distrito);
            })
            .catch(function () {
                setUbicacionAdministrativa("", "", "");
            });
    }

    function colocarPin(latitud, longitud, centrar) {
        if (!mapa) {
            return;
        }

        if (!marcador) {
            marcador = window.L.marker([latitud, longitud], { draggable: true }).addTo(mapa);
            marcador.on("dragend", function (evento) {
                var pos = evento.target.getLatLng();
                setCoordenadas(pos.lat, pos.lng);
                resolverUbicacionAdministrativa(pos.lat, pos.lng);
            });
        } else {
            marcador.setLatLng([latitud, longitud]);
        }

        if (centrar) {
            mapa.setView([latitud, longitud], Math.max(mapa.getZoom(), 13));
        }

        setCoordenadas(latitud, longitud);
        resolverUbicacionAdministrativa(latitud, longitud);
    }

    function inicializarMapa() {
        if (!mapaContenedor || typeof window.L === "undefined") {
            return;
        }

        mapa = window.L.map("mapaUbicacionFinca", {
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            tap: false
        }).setView([9.7489, -83.7534], 8);

        window.L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(mapa);

        mapa.on("click", function (evento) {
            colocarPin(evento.latlng.lat, evento.latlng.lng, true);
        });

        setTimeout(function () {
            mapa.invalidateSize();
        }, 120);
    }

    if (tieneRiosOQuebradasCheck) {
        tieneRiosOQuebradasCheck.addEventListener("change", sincronizarRecursosHidricos);
    }

    if (tieneNacientesCheck) {
        tieneNacientesCheck.addEventListener("change", sincronizarRecursosHidricos);
    }

    configurarMensajesValidacionEspanol();
    inicializarMapa();
    sincronizarRecursosHidricos();

    if (latitudInput && longitudInput && latitudInput.value && longitudInput.value && mapa) {
        colocarPin(Number(latitudInput.value), Number(longitudInput.value), true);
    }

    formulario.addEventListener("submit", function (evento) {
        var latitud = latitudInput ? Number(latitudInput.value) : NaN;
        var longitud = longitudInput ? Number(longitudInput.value) : NaN;
        var hectareasInput = formulario.querySelector("#Hectareas");
        var hectareas = hectareasInput ? Number(hectareasInput.value) : 0;
        var cantidadNacientes = cantidadNacientesInput ? Number(cantidadNacientesInput.value) : 0;
        var nacientesValidas = !tieneNacientesCheck || !tieneNacientesCheck.checked
            ? true
            : Number.isInteger(cantidadNacientes) && cantidadNacientes > 0;

        var coordenadasValidas = Number.isFinite(latitud)
            && Number.isFinite(longitud)
            && latitud >= -90 && latitud <= 90
            && longitud >= -180 && longitud <= 180;

        var ubicacionAdministrativaCompleta = provinciaInput && cantonInput && distritoInput
            && provinciaInput.value.trim()
            && cantonInput.value.trim()
            && distritoInput.value.trim();

        if (hectareasInput) {
            hectareasInput.setCustomValidity("");
            if (!Number.isFinite(hectareas) || hectareas <= 0) {
                hectareasInput.setCustomValidity("El valor de hectáreas debe ser mayor a 0.");
            }
        }

        if (!ubicacionAdministrativaCompleta) {
            if (provinciaInput) {
                provinciaInput.setCustomValidity("Seleccione un punto válido en el mapa para derivar la ubicación.");
            }
        }

        if (!formulario.checkValidity() || !coordenadasValidas || !Number.isFinite(hectareas) || hectareas <= 0 || !nacientesValidas) {
            evento.preventDefault();
            evento.stopPropagation();
            formulario.classList.add("was-validated");
            return;
        }

        if (!boton) {
            return;
        }

        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");

        if (textoBoton) {
            textoBoton.textContent = "Procesando...";
        }

        if (spinnerBoton) {
            spinnerBoton.classList.remove("d-none");
        }

        if (window.psa && typeof window.psa.mostrarTrobberGlobal === "function") {
            window.psa.mostrarTrobberGlobal();
        }
    });
});
