document.addEventListener("DOMContentLoaded", function () {
    const formulario = document.getElementById("formularioRegistrarFinca");

    if (!formulario) {
        return;
    }

    formulario.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        const finca = {
            idFinca: 0,
            idPropietario: parseInt(document.getElementById("idPropietario").value) || 0,
            nombreFinca: document.getElementById("nombreFinca").value.trim(),
            provincia: document.getElementById("provincia").value,
            canton: document.getElementById("canton").value.trim(),
            distrito: document.getElementById("distrito").value.trim(),
            direccionExacta: document.getElementById("direccionExacta").value.trim(),
            latitud: parseFloat(document.getElementById("latitud").value) || 0,
            longitud: parseFloat(document.getElementById("longitud").value) || 0,
            hectareas: parseFloat(document.getElementById("hectareas").value) || 0,
            vegetacion: document.getElementById("vegetacion").value,
            tieneRecursosHidricos: document.getElementById("tieneRecursosHidricos").value === "true",
            usoSuelo: document.getElementById("usoSuelo").value.trim(),
            pendiente: document.getElementById("pendiente").value.trim(),
            estadoFinca: document.getElementById("estadoFinca").value,
            fechaRegistro: new Date().toISOString(),
            fechaActualizacion: new Date().toISOString()
        };

        if (!validarFormulario(finca)) {
            return;
        }

        try {
            const respuesta = await fetch("https://localhost:7113/api/Finca/Create", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(finca)
            });

            if (!respuesta.ok) {
                const errorTexto = await respuesta.text();
                console.error("Error del API:", errorTexto);
                alert("No se pudo guardar la finca.");
                return;
            }

            alert("Finca guardada correctamente.");
            formulario.reset();
        } catch (error) {
            console.error("Error al conectar con el API:", error);
            alert("Error de conexión con el API.");
        }
    });

    function validarFormulario(finca) {
        if (finca.idPropietario <= 0) {
            alert("Debes ingresar un Id de propietario válido.");
            return false;
        }

        if (!finca.nombreFinca) {
            alert("Debes ingresar el nombre de la finca.");
            return false;
        }

        if (!finca.provincia) {
            alert("Debes seleccionar la provincia.");
            return false;
        }

        if (!finca.canton) {
            alert("Debes ingresar el cantón.");
            return false;
        }

        if (!finca.distrito) {
            alert("Debes ingresar el distrito.");
            return false;
        }

        if (finca.latitud < -90 || finca.latitud > 90) {
            alert("La latitud debe estar entre -90 y 90.");
            return false;
        }

        if (finca.longitud < -180 || finca.longitud > 180) {
            alert("La longitud debe estar entre -180 y 180.");
            return false;
        }

        if (finca.hectareas <= 0) {
            alert("Las hectáreas deben ser mayores a 0.");
            return false;
        }

        if (!finca.vegetacion) {
            alert("Debes seleccionar la vegetación.");
            return false;
        }

        if (!finca.usoSuelo) {
            alert("Debes ingresar el uso de suelo.");
            return false;
        }

        if (!finca.pendiente) {
            alert("Debes ingresar la pendiente.");
            return false;
        }

        if (finca.estadoFinca !== "Inactiva" && finca.estadoFinca !== "Rechazada") {
            alert("El estado de la finca debe ser Inactiva o Rechazada.");
            return false;
        }

        return true;
    }
});