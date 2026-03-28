using Microsoft.AspNetCore.Mvc;
using PSA.AppCore;
using PSA.EntidadesDTO.DTOs.Fincas;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FincaController : ControllerBase
    {
        private readonly FincaService _fincaService;

        public FincaController(FincaService fincaService)
        {
            _fincaService = fincaService;
        }

        [HttpGet("RetrieveAll")]
        public IActionResult RetrieveAll()
        {
            try
            {
                var response = _fincaService.RetrieveAll();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ocurrió un error al consultar las fincas.",
                    detail = ex.Message
                });
            }
        }

        [HttpGet("RetrieveById/{id}")]
        public IActionResult RetrieveById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        message = "El id de la finca debe ser mayor a 0."
                    });
                }

                var response = _fincaService.RetrieveById(id);

                if (response == null)
                {
                    return NotFound(new
                    {
                        message = "No se encontró la finca solicitada."
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ocurrió un error al consultar la finca.",
                    detail = ex.Message
                });
            }
        }

        [HttpPost("Create")]
        public IActionResult Create([FromBody] FincaDTO finca)
        {
            try
            {
                if (finca == null)
                {
                    return BadRequest(new
                    {
                        message = "Debe enviar la información de la finca."
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var errores = ValidarFinca(finca);

                if (errores.Any())
                {
                    return BadRequest(new
                    {
                        message = "La información de la finca no es válida.",
                        errors = errores
                    });
                }

                _fincaService.Create(finca);

                return Ok(new
                {
                    message = "Finca creada correctamente."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "No se pudo crear la finca.",
                    detail = ex.Message
                });
            }
        }

        [HttpPut("Update")]
        public IActionResult Update([FromBody] FincaDTO finca)
        {
            try
            {
                if (finca == null)
                {
                    return BadRequest(new
                    {
                        message = "Debe enviar la información de la finca."
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (finca.IdFinca <= 0)
                {
                    return BadRequest(new
                    {
                        message = "El id de la finca es obligatorio para actualizar."
                    });
                }

                var errores = ValidarFinca(finca);

                if (errores.Any())
                {
                    return BadRequest(new
                    {
                        message = "La información de la finca no es válida.",
                        errors = errores
                    });
                }

                _fincaService.Update(finca);

                return Ok(new
                {
                    message = "Finca actualizada correctamente."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "No se pudo actualizar la finca.",
                    detail = ex.Message
                });
            }
        }

        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        message = "El id de la finca debe ser mayor a 0."
                    });
                }

                _fincaService.Delete(id);

                return Ok(new
                {
                    message = "Finca eliminada correctamente."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "No se pudo eliminar la finca.",
                    detail = ex.Message
                });
            }
        }

        private List<string> ValidarFinca(FincaDTO finca)
        {
            var errores = new List<string>();

            if (finca.IdPropietario <= 0)
                errores.Add("El propietario es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.NombreFinca))
                errores.Add("El nombre de la finca es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.Provincia))
                errores.Add("La provincia es obligatoria.");

            if (string.IsNullOrWhiteSpace(finca.Canton))
                errores.Add("El cantón es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.Distrito))
                errores.Add("El distrito es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.Vegetacion))
                errores.Add("La vegetación es obligatoria.");

            if (string.IsNullOrWhiteSpace(finca.UsoSuelo))
                errores.Add("El uso de suelo es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.Pendiente))
                errores.Add("La pendiente es obligatoria.");

            if (string.IsNullOrWhiteSpace(finca.EstadoFinca))
                errores.Add("El estado de la finca es obligatorio.");

            if (finca.Hectareas <= 0)
                errores.Add("Las hectáreas deben ser un número positivo mayor a 0.");

            if (finca.Latitud < -90 || finca.Latitud > 90)
                errores.Add("La latitud debe estar entre -90 y 90.");

            if (finca.Longitud < -180 || finca.Longitud > 180)
                errores.Add("La longitud debe estar entre -180 y 180.");

            return errores;
        }
    }
}