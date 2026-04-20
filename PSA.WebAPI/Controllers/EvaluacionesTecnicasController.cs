using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Evaluaciones;
using PSA.WebAPI.Extensions;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "3")]
    public class EvaluacionesTecnicasController : ControllerBase
    {
        private readonly EvaluacionTecnicaManager _evaluacionTecnicaManager;

        public EvaluacionesTecnicasController(EvaluacionTecnicaManager evaluacionTecnicaManager)
        {
            _evaluacionTecnicaManager = evaluacionTecnicaManager;
        }

        [HttpGet("bandeja-pendientes")]
        public async Task<IActionResult> ObtenerBandejaPendientes()
        {
            var resultado = await _evaluacionTecnicaManager.ObtenerBandejaPendienteAsync();
            return Ok(resultado);
        }

        [HttpGet("{idEvaluacion:int}/detalle")]
        public async Task<IActionResult> ObtenerDetalle([FromRoute] int idEvaluacion)
        {
            var detalle = await _evaluacionTecnicaManager.ObtenerDetalleAsync(idEvaluacion);
            if (detalle == null)
            {
                return NotFound(new { Mensaje = "No se encontró la evaluación solicitada." });
            }

            return Ok(detalle);
        }

        [HttpPut("{idEvaluacion:int}/asignar")]
        public async Task<IActionResult> AsignarIngeniero([FromRoute] int idEvaluacion, [FromBody] AsignarEvaluacionDTO dto)
        {
            var asignado = await _evaluacionTecnicaManager.AsignarIngenieroAsync(idEvaluacion, dto.IdIngeniero);
            if (!asignado)
            {
                return BadRequest(new { Mensaje = "No fue posible asignar la evaluación. Verifique que esté en estado Pendiente." });
            }

            return Ok(new { Mensaje = "Evaluación asignada y movida a En proceso." });
        }

        [HttpPut("{idEvaluacion:int}/resultado")]
        public async Task<IActionResult> RegistrarResultado([FromRoute] int idEvaluacion, [FromBody] RegistrarResultadoEvaluacionDTO dto)
        {
            try
            {
                var actualizado = await _evaluacionTecnicaManager.RegistrarResultadoAsync(idEvaluacion, dto, GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
                if (!actualizado)
                {
                    return BadRequest(new { Mensaje = "No fue posible registrar el resultado de evaluación. Verifique que la evaluación esté en estado Pendiente o En proceso." });
                }

                return Ok(new { Mensaje = "Resultado de evaluación registrado correctamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
        }

        [HttpPut("{idEvaluacion:int}/estado")]
        public async Task<IActionResult> AvanzarEstado([FromRoute] int idEvaluacion, [FromQuery] string estado)
        {
            var actualizado = await _evaluacionTecnicaManager.AvanzarEstadoAsync(idEvaluacion, estado);
            if (!actualizado)
            {
                return NotFound(new { Mensaje = "No se pudo actualizar el estado de la evaluación." });
            }

            return Ok(new { Mensaje = "Estado de evaluación actualizado correctamente." });
        }

        [HttpGet("reportes")]
        public async Task<IActionResult> ObtenerReporte(
            [FromQuery] int? anio = null,
            [FromQuery] int? mes = null,
            [FromQuery] string? estadoEvaluacion = null,
            [FromQuery] string? decisionTecnica = null,
            [FromQuery] int? idIngeniero = null)
        {
            try
            {
                var reporte = await _evaluacionTecnicaManager.ObtenerReporteEvaluacionesAsync(new FiltroReporteEvaluacionesDTO
                {
                    Anio = anio,
                    Mes = mes,
                    EstadoEvaluacion = estadoEvaluacion,
                    DecisionTecnica = decisionTecnica,
                    IdIngeniero = idIngeniero
                });

                return Ok(reporte);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Mensaje = "Ocurrió un error inesperado al consultar el reporte." });
            }
        }
    }
}
