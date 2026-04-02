using Microsoft.AspNetCore.Mvc;
using PSA.AppCore;
using PSA.EntidadesDTO.Entidades.Evaluaciones;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluacionController : ControllerBase
    {
        private readonly EvaluacionService _evaluacionService;

        public EvaluacionController(EvaluacionService evaluacionService)
        {
            _evaluacionService = evaluacionService;
        }

        [HttpPost("Crear")]
        public async Task<IActionResult> Crear([FromBody] EvaluacionTecnica evaluacion)
        {
            try
            {
                if (evaluacion == null)
                {
                    return BadRequest(new
                    {
                        message = "Debe enviar la información de la evaluación."
                    });
                }

                if (evaluacion.FincaId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "La finca es requerida."
                    });
                }

                if (evaluacion.IngenieroForestalId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "El ingeniero forestal es requerido."
                    });
                }

                if (evaluacion.FechaEvaluacion == default)
                {
                    return BadRequest(new
                    {
                        message = "La fecha de evaluación es requerida."
                    });
                }

                if (string.IsNullOrWhiteSpace(evaluacion.Estado))
                {
                    return BadRequest(new
                    {
                        message = "El estado de la evaluación es requerido."
                    });
                }

                var id = await _evaluacionService.CrearEvaluacionAsync(evaluacion);

                return Ok(new
                {
                    message = "Evaluación creada correctamente.",
                    idEvaluacion = id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "No se pudo crear la evaluación.",
                    detail = ex.Message
                });
            }
        }

        [HttpPut("Finalizar/{id}")]
        public async Task<IActionResult> Finalizar(int id, [FromBody] FinalizarEvaluacionRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        message = "El id de la evaluación es inválido."
                    });
                }

                if (request == null)
                {
                    return BadRequest(new
                    {
                        message = "Debe enviar la información de finalización."
                    });
                }

                await _evaluacionService.FinalizarEvaluacionAsync(
                    id,
                    request.Decision,
                    request.Observaciones
                );

                return Ok(new
                {
                    message = "Evaluación finalizada correctamente."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "No se pudo finalizar la evaluación.",
                    detail = ex.Message
                });
            }
        }
    }

    public class FinalizarEvaluacionRequest
    {
        public string Decision { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}