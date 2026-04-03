document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioRegistrarFinca");
    if (!formulario) {
        return;
    }

    var boton = formulario.querySelector("[data-loading-button]");
    var textoBoton = formulario.querySelector("[data-loading-texto]");
    var spinnerBoton = formulario.querySelector("[data-loading-spinner]");

    function obtenerCampo(id) {
        return document.getElementById(id) || null;
    }

    var paisInput = obtenerCampo("paisSeleccionado");
    var provinciaInput = obtenerCampo("provinciaInput");
    var cantonInput = obtenerCampo("cantonInput");
    var distritoInput = obtenerCampo("distritoInput");
    var latitudInput = obtenerCampo("Latitud");
    var longitudInput = obtenerCampo("Longitud");

    var tieneRiosOQuebradasCheck = obtenerCampo("tieneRiosOQuebradas");
    var tieneNacientesCheck = obtenerCampo("tieneNacientes");
    var cantidadNacientesInput = obtenerCampo("cantidadNacientes");
    var tieneRecursosHidricosInput = obtenerCampo("tieneRecursosHidricos");

    var mapaContenedor = obtenerCampo("mapaUbicacionFinca");
    var mapa = null;
    var marcador = null;
    var limitesCostaRica = null;

    function obtenerMensajeValidacion(campo) {
        var nombreCampo = (campo.labels && campo.labels[0] && campo.labels[0].textContent)
            ? campo.labels[0].textContent.trim()
            : "Este campo";

        if (campo.validity.valueMissing) return "El campo '" + nombreCampo + "' es obligatorio.";
        if (campo.validity.typeMismatch) return "El valor de '" + nombreCampo + "' no es válido.";
        if (campo.validity.rangeUnderflow || campo.validity.rangeOverflow) return "El valor de '" + nombreCampo + "' está fuera del rango permitido.";
        if (campo.validity.stepMismatch) return "El formato numérico de '" + nombreCampo + "' no es válido.";
        if (campo.validity.tooLong) return "El valor de '" + nombreCampo + "' supera la longitud permitida.";
        if (campo.validity.tooShort) return "El valor de '" + nombreCampo + "' es demasiado corto.";
        if (campo.validity.badInput) return "El valor ingresado en '" + nombreCampo + "' no tiene un formato válido.";

        return "";
    }

    function configurarMensajesValidacionEspanol() {
        formulario.querySelectorAll("input, select, textarea").forEach(function (campo) {
            campo.addEventListener("invalid", function () {
                campo.setCustomValidity(obtenerMensajeValidacion(campo));
            });
            campo.addEventListener("input", function () { campo.setCustomValidity(""); });
            campo.addEventListener("change", function () { campo.setCustomValidity(""); });
        });
    }

    function sincronizarRecursosHidricos() {
        if (!tieneRecursosHidricosInput) return;

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

    function setCoordenadas(latitud, longitud) {
        var lat = Number(latitud);
        var lon = Number(longitud);
        if (!Number.isFinite(lat) || !Number.isFinite(lon)) return;

        var latNorm = lat.toFixed(7);
        var lonNorm = lon.toFixed(7);

        if (latitudInput) latitudInput.value = latNorm;
        if (longitudInput) longitudInput.value = lonNorm;
    }

    function setUbicacionAdministrativa(provincia, canton, distrito) {
        if (paisInput) paisInput.value = "Costa Rica";
        if (provinciaInput) provinciaInput.value = provincia || "";
        if (cantonInput) cantonInput.value = canton || "";
        if (distritoInput) distritoInput.value = distrito || "";
    }

    function extraerPrimerValorValido() {
        for (var i = 0; i < arguments.length; i++) {
            var valor = arguments[i];
            if (valor && String(valor).trim()) return String(valor).trim();
        }
        return "";
    }

    function resolverUbicacionAdministrativa(latitud, longitud) {
        var url = "https://nominatim.openstreetmap.org/reverse?format=jsonv2&accept-language=es&zoom=18&lat="
            + encodeURIComponent(latitud)
            + "&lon="
            + encodeURIComponent(longitud);

        return fetch(url, { headers: { "Accept": "application/json" } })
            .then(function (respuesta) {
                if (!respuesta.ok) throw new Error("No se pudo resolver la ubicación administrativa");
                return respuesta.json();
            })
            .then(function (resultado) {
                var address = resultado && resultado.address ? resultado.address : {};
                var paisCodigo = extraerPrimerValorValido(address.country_code).toLowerCase();

                if (paisCodigo !== "cr") {
                    throw new Error("La ubicación seleccionada está fuera de Costa Rica");
                }

                var provincia = extraerPrimerValorValido(
                    address.state,
                    address.region,
                    address.province,
                    address.state_district
                ).replace(/^Provincia de\s+/i, "").trim();

                var canton = extraerPrimerValorValido(
                    address.county,
                    address.city,
                    address.town,
                    address.municipality,
                    address.state_district
                ).replace(/^Cant[oó]n de\s+/i, "").trim();

                var distrito = extraerPrimerValorValido(
                    address.city_district,
                    address.suburb,
                    address.village,
                    address.hamlet,
                    address.neighbourhood,
                    address.quarter
                ).replace(/^Distrito de\s+/i, "").trim();

                setUbicacionAdministrativa(provincia, canton, distrito);
                if (provinciaInput) provinciaInput.setCustomValidity("");
            })
            .catch(function () {
                setUbicacionAdministrativa("", "", "");
                if (provinciaInput) {
                    provinciaInput.setCustomValidity("Seleccione un punto dentro del territorio de Costa Rica.");
                }
            });
    }

    function colocarPin(latitud, longitud, centrar) {
        if (!mapa || typeof window.L === "undefined") return;

        var latLng = window.L.latLng(latitud, longitud);
        if (limitesCostaRica && !limitesCostaRica.contains(latLng)) {
            if (provinciaInput) {
                provinciaInput.setCustomValidity("Seleccione un punto dentro del territorio de Costa Rica.");
            }
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
        if (!mapaContenedor || typeof window.L === "undefined") return;

        limitesCostaRica = window.L.latLngBounds(
            window.L.latLng(8.0, -86.2),
            window.L.latLng(11.4, -82.3)
        );

        mapa = window.L.map("mapaUbicacionFinca", {
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            tap: false
        }).setView([9.9325, -84.08], 11);

        window.L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(mapa);

        mapa.on("click", function (evento) {
            colocarPin(evento.latlng.lat, evento.latlng.lng, true);
        });

        setTimeout(function () { mapa.invalidateSize(); }, 120);

        if (latitudInput && longitudInput && latitudInput.value && longitudInput.value) {
            var latitudInicial = Number(latitudInput.value);
            var longitudInicial = Number(longitudInput.value);
            var coordenadasInicialesValidas = Number.isFinite(latitudInicial)
                && Number.isFinite(longitudInicial)
                && latitudInicial >= -90 && latitudInicial <= 90
                && longitudInicial >= -180 && longitudInicial <= 180
                && !(latitudInicial === 0 && longitudInicial === 0);

            if (coordenadasInicialesValidas) {
                colocarPin(latitudInicial, longitudInicial, true);
            }
        }
    }

    if (tieneRiosOQuebradasCheck) {
        tieneRiosOQuebradasCheck.addEventListener("change", sincronizarRecursosHidricos);
    }
    if (tieneNacientesCheck) {
        tieneNacientesCheck.addEventListener("change", sincronizarRecursosHidricos);
    }

    configurarMensajesValidacionEspanol();
    formulario.setAttribute("lang", "es");
    sincronizarRecursosHidricos();
    inicializarMapa();

    formulario.addEventListener("submit", function (evento) {
        var latitud = latitudInput ? Number(latitudInput.value) : NaN;
        var longitud = longitudInput ? Number(longitudInput.value) : NaN;
        var hectareasInput = formulario.querySelector("#Hectareas");
        var hectareas = hectareasInput ? Number(hectareasInput.value) : 0;
        var cantidadNacientes = cantidadNacientesInput ? Number(cantidadNacientesInput.value) : 0;

        var coordenadasValidas = Number.isFinite(latitud)
            && Number.isFinite(longitud)
            && latitud >= -90 && latitud <= 90
            && longitud >= -180 && longitud <= 180;

        var nacientesValidas = !tieneNacientesCheck || !tieneNacientesCheck.checked
            ? true
            : Number.isInteger(cantidadNacientes) && cantidadNacientes > 0;

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

        if (!ubicacionAdministrativaCompleta && provinciaInput) {
            provinciaInput.setCustomValidity("Seleccione un punto válido en el mapa para derivar la ubicación.");
        }

        if (!formulario.checkValidity() || !coordenadasValidas || !Number.isFinite(hectareas) || hectareas <= 0 || !nacientesValidas) {
            evento.preventDefault();
            evento.stopPropagation();
            formulario.classList.add("was-validated");
            return;
        }

        if (!boton) return;

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
