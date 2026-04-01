document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioRegistrarFinca");
    if (!formulario) {
        return;
    }

    var boton = formulario.querySelector("[data-loading-button]");
    var textoBoton = formulario.querySelector("[data-loading-texto]");
    var spinnerBoton = formulario.querySelector("[data-loading-spinner]");

    function obtenerCampo(principalId, alternoNombre) {
        return document.getElementById(principalId)
            || formulario.querySelector("[name='" + alternoNombre + "']");
    }

    var paisInput = document.getElementById("paisInput");
    var provinciaInput = obtenerCampo("provinciaInput", "Provincia");
    var cantonInput = obtenerCampo("cantonInput", "Canton");
    var distritoInput = obtenerCampo("distritoInput", "Distrito");
    var latitudInput = obtenerCampo("Latitud", "Latitud");
    var longitudInput = obtenerCampo("Longitud", "Longitud");
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

    var ubicacionesCR = {
        "San José": {
            "San José": ["Carmen", "Merced", "Hospital", "Catedral", "Zapote", "San Francisco de Dos Ríos", "Uruca", "Mata Redonda", "Pavas", "Hatillo", "San Sebastián"],
            "Escazú": ["Escazú", "San Antonio", "San Rafael"],
            "Desamparados": ["Desamparados", "San Miguel", "San Juan de Dios", "San Rafael Arriba", "San Antonio", "Frailes", "Patarrá", "San Cristóbal", "Rosario", "Damas", "San Rafael Abajo", "Gravilias", "Los Guido"],
            "Puriscal": ["Santiago", "Mercedes Sur", "Barbacoas", "Grifo Alto", "San Rafael", "Candelarita", "Desamparaditos", "San Antonio", "Chires"],
            "Tarrazú": ["San Marcos", "San Lorenzo", "San Carlos"],
            "Aserrí": ["Aserrí", "Tarbaca", "Vuelta de Jorco", "San Gabriel", "Legua", "Monterrey", "Salitrillos"],
            "Mora": ["Colón", "Guayabo", "Tabarcia", "Piedras Negras", "Picagres", "Jaris", "Quitirrisí"],
            "Goicoechea": ["Guadalupe", "San Francisco", "Calle Blancos", "Mata de Plátano", "Ipís", "Rancho Redondo", "Purral"],
            "Santa Ana": ["Santa Ana", "Salitral", "Pozos", "Uruca", "Piedades", "Brasil"],
            "Alajuelita": ["Alajuelita", "San Josecito", "San Antonio", "Concepción", "San Felipe"],
            "Vázquez de Coronado": ["San Isidro", "San Rafael", "Dulce Nombre de Jesús", "Patalillo", "Cascajal"],
            "Acosta": ["San Ignacio", "Guaitil", "Palmichal", "Cangrejal", "Sabanillas"],
            "Tibás": ["San Juan", "Cinco Esquinas", "Anselmo Llorente", "León XIII", "Colima"],
            "Moravia": ["San Vicente", "San Jerónimo", "La Trinidad"],
            "Montes de Oca": ["San Pedro", "Sabanilla", "Mercedes", "San Rafael"],
            "Turrubares": ["San Pablo", "San Pedro", "San Juan de Mata", "San Luis", "Carara"],
            "Dota": ["Santa María", "Jardín", "Copey"],
            "Curridabat": ["Curridabat", "Granadilla", "Sánchez", "Tirrases"],
            "Pérez Zeledón": ["San Isidro de El General", "El General", "Daniel Flores", "Rivas", "San Pedro", "Platanar", "Pejibaye", "Cajón", "Barú", "Río Nuevo", "Páramo", "La Amistad"],
            "León Cortés Castro": ["San Pablo", "San Andrés", "Llano Bonito", "San Isidro", "Santa Cruz", "San Antonio"]
        },
        "Alajuela": {
            "Alajuela": ["Alajuela", "San José", "Carrizal", "San Antonio", "Guácima", "San Isidro", "Sabanilla", "San Rafael", "Río Segundo", "Desamparados", "Turrúcares", "Tambor", "Garita", "Sarapiquí"],
            "San Ramón": ["San Ramón", "Santiago", "San Juan", "Piedades Norte", "Piedades Sur", "San Rafael", "San Isidro", "Ángeles", "Alfaro", "Volio", "Concepción", "Zapotal", "Peñas Blancas", "San Lorenzo"],
            "Grecia": ["Grecia", "San Isidro", "San José", "San Roque", "Tacares", "Puente de Piedra", "Bolívar"],
            "San Mateo": ["San Mateo", "Desmonte", "Jesús María", "Labrador"],
            "Atenas": ["Atenas", "Jesús", "Mercedes", "San Isidro", "Concepción", "San José", "Santa Eulalia", "Escobal"],
            "Naranjo": ["Naranjo", "San Miguel", "San José", "Cirrí Sur", "San Jerónimo", "San Juan", "El Rosario", "Palmitos"],
            "Palmares": ["Palmares", "Zaragoza", "Buenos Aires", "Santiago", "Candelaria", "Esquipulas", "La Granja"],
            "Poás": ["San Pedro", "San Juan", "San Rafael", "Carrillos", "Sabana Redonda"],
            "Orotina": ["Orotina", "El Mastate", "Hacienda Vieja", "Coyolar", "La Ceiba"],
            "San Carlos": ["Quesada", "Florencia", "Buenavista", "Aguas Zarcas", "Venecia", "Pital", "La Fortuna", "La Tigra", "La Palmera", "Venado", "Cutris", "Monterrey", "Pocosol"],
            "Zarcero": ["Zarcero", "Laguna", "Tapezco", "Guadalupe", "Palmira", "Zapote", "Brisas"],
            "Sarchí": ["Sarchí Norte", "Sarchí Sur", "Toro Amarillo", "San Pedro", "Rodríguez"],
            "Upala": ["Upala", "Aguas Claras", "San José o Pizote", "Bijagua", "Delicias", "Dos Ríos", "Yolillal", "Canalete"],
            "Los Chiles": ["Los Chiles", "Caño Negro", "El Amparo", "San Jorge"],
            "Guatuso": ["San Rafael", "Buenavista", "Cote", "Katira"],
            "Río Cuarto": ["Río Cuarto", "Santa Rita", "Santa Isabel"]
        },
        "Cartago": {
            "Cartago": ["Oriental", "Occidental", "Carmen", "San Nicolás", "Aguacaliente o San Francisco", "Guadalupe o Arenilla", "Corralillo", "Tierra Blanca", "Dulce Nombre", "Llano Grande", "Quebradilla"],
            "Paraíso": ["Paraíso", "Santiago", "Orosi", "Cachí", "Llanos de Santa Lucía", "Birrisito"],
            "La Unión": ["Tres Ríos", "San Diego", "San Juan", "San Rafael", "Concepción", "Dulce Nombre", "San Ramón", "Río Azul"],
            "Jiménez": ["Juan Viñas", "Tucurrique", "Pejibaye", "La Victoria"],
            "Turrialba": ["Turrialba", "La Suiza", "Peralta", "Santa Cruz", "Santa Teresita", "Pavones", "Tuis", "Tayutic", "Santa Rosa", "Tres Equis"],
            "Alvarado": ["Pacayas", "Cervantes", "Capellades"],
            "Oreamuno": ["San Rafael", "Cot", "Potrero Cerrado", "Cipreses", "Santa Rosa"],
            "El Guarco": ["El Tejar", "San Isidro", "Tobosí", "Patio de Agua"]
        },
        "Heredia": {
            "Heredia": ["Heredia", "Mercedes", "San Francisco", "Ulloa", "Varablanca"],
            "Barva": ["Barva", "San Pedro", "San Pablo", "San Roque", "Santa Lucía", "San José de la Montaña", "Puente Salas"],
            "Santo Domingo": ["Santo Domingo", "San Vicente", "San Miguel", "Paracito", "Santo Tomás", "Santa Rosa", "Tures", "Pará"],
            "Santa Bárbara": ["Santa Bárbara", "San Pedro", "San Juan", "Jesús", "Santo Domingo", "Purabá"],
            "San Rafael": ["San Rafael", "San Josecito", "Santiago", "Los Ángeles", "Concepción"],
            "San Isidro": ["San Isidro", "San José", "Concepción", "San Francisco"],
            "Belén": ["San Antonio", "La Ribera", "La Asunción"],
            "Flores": ["San Joaquín", "Barrantes", "Llorente"],
            "San Pablo": ["San Pablo", "Rincón de Sabanilla"],
            "Sarapiquí": ["Puerto Viejo", "La Virgen", "Las Horquetas", "Llanuras del Gaspar", "Cureña"]
        },
        "Guanacaste": {
            "Liberia": ["Liberia", "Cañas Dulces", "Mayorga", "Nacascolo", "Curubandé"],
            "Nicoya": ["Nicoya", "Mansión", "San Antonio", "Quebrada Honda", "Sámara", "Nosara", "Belén de Nosarita"],
            "Santa Cruz": ["Santa Cruz", "Bolsón", "Veintisiete de Abril", "Tempate", "Cartagena", "Cuajiniquil", "Diriá", "Cabo Velas", "Tamarindo"],
            "Bagaces": ["Bagaces", "La Fortuna", "Mogote", "Río Naranjo"],
            "Carrillo": ["Filadelfia", "Palmira", "Sardinal", "Belén"],
            "Cañas": ["Cañas", "Palmira", "San Miguel", "Bebedero", "Porozal"],
            "Abangares": ["Las Juntas", "Sierra", "San Juan", "Colorado"],
            "Tilarán": ["Tilarán", "Quebrada Grande", "Tronadora", "Santa Rosa", "Líbano", "Tierras Morenas", "Arenal", "Cabeceras"],
            "Nandayure": ["Carmona", "Santa Rita", "Zapotal", "San Pablo", "Porvenir", "Bejuco"],
            "La Cruz": ["La Cruz", "Santa Cecilia", "La Garita", "Santa Elena"],
            "Hojancha": ["Hojancha", "Monte Romo", "Puerto Carrillo", "Huacas", "Matambú"]
        },
        "Puntarenas": {
            "Puntarenas": ["Puntarenas", "Pitahaya", "Chomes", "Lepanto", "Paquera", "Manzanillo", "Guacimal", "Barranca", "Isla del Coco", "Cóbano", "Chacarita", "Chira", "Acapulco", "El Roble", "Arancibia"],
            "Esparza": ["Espíritu Santo", "San Juan Grande", "Macacona", "San Rafael", "San Jerónimo", "Caldera"],
            "Buenos Aires": ["Buenos Aires", "Volcán", "Potrero Grande", "Boruca", "Pilas", "Colinas", "Chánguena", "Biolley", "Brunka"],
            "Montes de Oro": ["Miramar", "La Unión", "San Isidro"],
            "Osa": ["Puerto Cortés", "Palmar", "Sierpe", "Bahía Ballena", "Piedras Blancas", "Bahía Drake"],
            "Quepos": ["Quepos", "Savegre", "Naranjito"],
            "Golfito": ["Golfito", "Guaycará", "Pavón"],
            "Coto Brus": ["San Vito", "Sabalito", "Aguabuena", "Limoncito", "Pittier", "Gutiérrez Braun"],
            "Parrita": ["Parrita"],
            "Corredores": ["Corredor", "La Cuesta", "Canoas", "Laurel"],
            "Garabito": ["Jacó", "Tárcoles", "Lagunillas"],
            "Monteverde": ["Monteverde"],
            "Puerto Jiménez": ["Puerto Jiménez"]
        },
        "Limón": {
            "Limón": ["Limón", "Valle La Estrella", "Río Blanco", "Matama"],
            "Pococí": ["Guápiles", "Jiménez", "Rita", "Roxana", "Cariari", "Colorado", "La Colonia"],
            "Siquirres": ["Siquirres", "Pacuarito", "Florida", "Germania", "El Cairo", "Alegría", "Reventazón"],
            "Talamanca": ["Bratsi", "Sixaola", "Cahuita", "Telire"],
            "Matina": ["Matina", "Batán", "Carrandi"],
            "Guácimo": ["Guácimo", "Mercedes", "Pocora", "Río Jiménez", "Duacarí"]
        }
    };

    var centrosProvincia = {
        "San José": [9.9281, -84.0907],
        "Alajuela": [10.0162, -84.2116],
        "Cartago": [9.8644, -83.9194],
        "Heredia": [9.9980, -84.1165],
        "Guanacaste": [10.6350, -85.4377],
        "Puntarenas": [9.9762, -84.8384],
        "Limón": [9.9907, -83.0359]
    };

    function llenarOpciones(select, opciones, placeholder) {
        if (!select) {
            return;
        }

        select.innerHTML = "";

        var opcionInicial = document.createElement("option");
        opcionInicial.value = "";
        opcionInicial.textContent = placeholder;
        select.appendChild(opcionInicial);

        opciones.forEach(function (opcion) {
            var item = document.createElement("option");
            item.value = opcion;
            item.textContent = opcion;
            select.appendChild(item);
        });
    }

    function inicializarUbicaciones() {
        var provincias = Object.keys(ubicacionesCR);
        llenarOpciones(provinciaSelect, provincias, "Seleccione una provincia");

        var provinciaActual = provinciaSelect.dataset.valorActual || provinciaSelect.value;
        var cantonActual = cantonSelect.dataset.valorActual || cantonSelect.value;
        var distritoActual = distritoSelect.dataset.valorActual || distritoSelect.value;

        if (!provinciaActual) {
            provinciaActual = "San José";
        }

        if (provinciaActual && ubicacionesCR[provinciaActual]) {
            provinciaSelect.value = provinciaActual;
            cargarCantones(provinciaActual, cantonActual, distritoActual);
            var centroInicial = centrosProvincia[provinciaActual];
            if (mapa && centroInicial) {
                mapa.setView(centroInicial, 11);
            }
        } else {
            cantonSelect.disabled = true;
            distritoSelect.disabled = true;
            llenarOpciones(cantonSelect, [], "Seleccione primero una provincia");
            llenarOpciones(distritoSelect, [], "Seleccione primero un cantón");
        }
    }

    function cargarCantones(provincia, cantonSeleccionado, distritoSeleccionado) {
        var cantones = Object.keys(ubicacionesCR[provincia] || {});
        llenarOpciones(cantonSelect, cantones, "Seleccione un cantón");
        cantonSelect.disabled = cantones.length === 0;

        if (cantonSeleccionado && cantones.indexOf(cantonSeleccionado) >= 0) {
            cantonSelect.value = cantonSeleccionado;
            cargarDistritos(provincia, cantonSeleccionado, distritoSeleccionado);
            return;
        }

        llenarOpciones(distritoSelect, [], "Seleccione primero un cantón");
        distritoSelect.disabled = true;
    }

    function cargarDistritos(provincia, canton, distritoSeleccionado) {
        var distritos = ((ubicacionesCR[provincia] || {})[canton]) || [];
        llenarOpciones(distritoSelect, distritos, "Seleccione un distrito");
        distritoSelect.disabled = distritos.length === 0;

        if (distritoSeleccionado && distritos.indexOf(distritoSeleccionado) >= 0) {
            distritoSelect.value = distritoSeleccionado;
        }
    }

    var mapaContenedor = document.getElementById("mapaUbicacionFinca");
    var mapa = null;
    var marcador = null;
    var limitesCostaRica = null;

    function inicializarMapa() {
        if (!mapaContenedor || typeof window.L === "undefined") {
            return;
        }

        limitesCostaRica = window.L.latLngBounds(
            window.L.latLng(8.0, -86.2),
            window.L.latLng(11.4, -82.3)
        );

        mapa = window.L.map("mapaUbicacionFinca", {
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            tap: false,
            maxBounds: limitesCostaRica,
            maxBoundsViscosity: 1.0
        }).setView([9.9281, -84.0907], 11);

        window.L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(mapa);

        mapa.on("click", function (evento) {
            if (limitesCostaRica && !limitesCostaRica.contains(evento.latlng)) {
                if (latitudInput) {
                    latitudInput.setCustomValidity("Seleccione un punto dentro de Costa Rica.");
                }
                return;
            }

            if (latitudInput) {
                latitudInput.setCustomValidity("");
            }
            colocarPin(evento.latlng.lat, evento.latlng.lng, true);
        });

        setTimeout(function () {
            mapa.invalidateSize();
        }, 120);
    }

    function colocarPin(latitud, longitud, centrar) {
        if (!mapa) {
            return;
        }

        if (limitesCostaRica && !limitesCostaRica.contains(window.L.latLng(latitud, longitud))) {
            return;
        }

        if (!marcador) {
            marcador = window.L.marker([latitud, longitud], { draggable: true }).addTo(mapa);
            marcador.on("dragend", function (evento) {
                var pos = evento.target.getLatLng();
                setCoordenadas(pos.lat, pos.lng);
            });
        } else {
            marcador.setLatLng([latitud, longitud]);
        }

        if (centrar) {
            mapa.setView([latitud, longitud], Math.max(mapa.getZoom(), 13));
        }

        setCoordenadas(latitud, longitud);
    }

    function setCoordenadas(latitud, longitud) {
        var latitudNormalizada = Number(latitud).toFixed(7);
        var longitudNormalizada = Number(longitud).toFixed(7);

        if (latitudInput) {
            latitudInput.value = latitudNormalizada;
            latitudInput.setAttribute("value", latitudNormalizada);
        }
        if (longitudInput) {
            longitudInput.value = longitudNormalizada;
            longitudInput.setAttribute("value", longitudNormalizada);
        }
    }

    function setUbicacionAdministrativa(provincia, canton, distrito) {
        if (paisInput) {
            paisInput.value = "Costa Rica";
        }
        if (provinciaInput) {
            provinciaInput.value = provincia || "";
            provinciaInput.setCustomValidity("");
        }
        if (cantonInput) {
            cantonInput.value = canton || "";
            cantonInput.setCustomValidity("");
        }
        if (distritoInput) {
            distritoInput.value = distrito || "";
            distritoInput.setCustomValidity("");
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
            })
            .catch(function () {
                setUbicacionAdministrativa("", "", "");
                if (provinciaInput) {
                    provinciaInput.setCustomValidity("Seleccione un punto dentro del territorio de Costa Rica.");
                }
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

        var limitesCostaRica = window.L.latLngBounds(
            window.L.latLng(8.0, -86.2),
            window.L.latLng(11.4, -82.3)
        );

        mapa = window.L.map("mapaUbicacionFinca", {
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            tap: false,
            maxBounds: limitesCostaRica,
            maxBoundsViscosity: 1.0
        }).setView([9.7489, -83.7534], 8);

        window.L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(mapa);

        mapa.on("click", function (evento) {
            if (!limitesCostaRica.contains(evento.latlng)) {
                return;
            }
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
    setUbicacionAdministrativa("", "", "");
    inicializarMapa();
    sincronizarRecursosHidricos();

    if (latitudInput && longitudInput && latitudInput.value && longitudInput.value && mapa) {
        var latInicial = Number(latitudInput.value);
        var lonInicial = Number(longitudInput.value);
        if (Number.isFinite(latInicial) && Number.isFinite(lonInicial) && (Math.abs(latInicial) > 0.000001 || Math.abs(lonInicial) > 0.000001)) {
            colocarPin(latInicial, lonInicial, true);
        }
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
