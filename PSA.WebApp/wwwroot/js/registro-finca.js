document.addEventListener("DOMContentLoaded", function () {
    var formulario = document.getElementById("formularioRegistrarFinca");
    if (!formulario) {
        return;
    }

    var boton = formulario.querySelector("[data-loading-button]");
    var textoBoton = formulario.querySelector("[data-loading-texto]");
    var spinnerBoton = formulario.querySelector("[data-loading-spinner]");

    var provinciaSelect = document.getElementById("provinciaSelect");
    var cantonSelect = document.getElementById("cantonSelect");
    var distritoSelect = document.getElementById("distritoSelect");

    var catalogoUbicaciones = {
        "San José": {
            "San José": ["Carmen", "Merced", "Hospital", "Mata Redonda", "Pavas"],
            "Escazú": ["Escazú", "San Antonio", "San Rafael"],
            "Desamparados": ["Desamparados", "San Miguel", "San Juan de Dios"]
        },
        "Alajuela": {
            "Alajuela": ["Alajuela", "San José", "Carrizal", "San Antonio"],
            "San Ramón": ["San Ramón", "Santiago", "San Juan"],
            "Grecia": ["Grecia", "San Isidro", "San José"]
        },
        "Cartago": {
            "Cartago": ["Oriental", "Occidental", "Carmen", "San Nicolás"],
            "Paraíso": ["Paraíso", "Santiago", "Orosi"],
            "La Unión": ["Tres Ríos", "San Diego", "Concepción"]
        },
        "Heredia": {
            "Heredia": ["Heredia", "Mercedes", "San Francisco", "Ulloa"],
            "Barva": ["Barva", "San Pedro", "San Pablo"],
            "Santo Domingo": ["Santo Domingo", "San Vicente", "Paracito"]
        },
        "Guanacaste": {
            "Liberia": ["Liberia", "Cañas Dulces", "Nacascolo"],
            "Nicoya": ["Nicoya", "Mansión", "San Antonio"],
            "Santa Cruz": ["Santa Cruz", "Bolsón", "Veintisiete de Abril"]
        },
        "Puntarenas": {
            "Puntarenas": ["Puntarenas", "Pitahaya", "Chomes", "Lepanto"],
            "Esparza": ["Espíritu Santo", "San Juan Grande", "Macacona"],
            "Buenos Aires": ["Buenos Aires", "Volcán", "Potrero Grande"]
        },
        "Limón": {
            "Limón": ["Limón", "Valle La Estrella", "Río Blanco", "Matama"],
            "Pococí": ["Guápiles", "Jiménez", "Rita", "Roxana"],
            "Siquirres": ["Siquirres", "Pacuarito", "Florida"]
        }
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
        if (!provinciaSelect || !cantonSelect || !distritoSelect) {
            return;
        }

        var provincias = Object.keys(catalogoUbicaciones);
        llenarOpciones(provinciaSelect, provincias, "Seleccione una provincia");

        var provinciaActual = provinciaSelect.dataset.valorActual || provinciaSelect.value;
        var cantonActual = cantonSelect.dataset.valorActual || cantonSelect.value;
        var distritoActual = distritoSelect.dataset.valorActual || distritoSelect.value;

        if (provinciaActual && catalogoUbicaciones[provinciaActual]) {
            provinciaSelect.value = provinciaActual;
            cargarCantones(provinciaActual, cantonActual, distritoActual);
        } else {
            cantonSelect.disabled = true;
            distritoSelect.disabled = true;
            llenarOpciones(cantonSelect, [], "Seleccione primero una provincia");
            llenarOpciones(distritoSelect, [], "Seleccione primero un cantón");
        }
    }

    function cargarCantones(provincia, cantonSeleccionado, distritoSeleccionado) {
        var cantones = Object.keys(catalogoUbicaciones[provincia] || {});
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
        var distritos = ((catalogoUbicaciones[provincia] || {})[canton]) || [];
        llenarOpciones(distritoSelect, distritos, "Seleccione un distrito");
        distritoSelect.disabled = distritos.length === 0;

        if (distritoSeleccionado && distritos.indexOf(distritoSeleccionado) >= 0) {
            distritoSelect.value = distritoSeleccionado;
        }
    }

    provinciaSelect.addEventListener("change", function () {
        if (!provinciaSelect.value) {
            llenarOpciones(cantonSelect, [], "Seleccione primero una provincia");
            llenarOpciones(distritoSelect, [], "Seleccione primero un cantón");
            cantonSelect.disabled = true;
            distritoSelect.disabled = true;
            return;
        }

        cargarCantones(provinciaSelect.value);
    });

    cantonSelect.addEventListener("change", function () {
        if (!provinciaSelect.value || !cantonSelect.value) {
            llenarOpciones(distritoSelect, [], "Seleccione primero un cantón");
            distritoSelect.disabled = true;
            return;
        }

        cargarDistritos(provinciaSelect.value, cantonSelect.value);
    });

    inicializarUbicaciones();

    formulario.addEventListener("submit", function (evento) {
        var hectareasInput = formulario.querySelector("#Hectareas");
        var hectareas = hectareasInput ? Number(hectareasInput.value) : 0;

        if (!formulario.checkValidity() || !Number.isFinite(hectareas) || hectareas <= 0) {
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
