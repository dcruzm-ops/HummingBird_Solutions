using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FincasController : ControllerBase
    {
        private readonly FincaManager _fincaManager;

        public FincasController(FincaManager fincaManager)
        {
            _fincaManager = fincaManager;
        }

        [HttpGet("mis-fincas")]
        public async Task<IActionResult> ObtenerMisFincas([FromQuery] int idPropietario)
        {
            try
            {
                if (idPropietario <= 0) return BadRequest(new { Mensaje = "El idPropietario debe ser mayor a 0." });
                return Ok(await _fincaManager.ObtenerPorPropietarioAsync(idPropietario));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = $"Error al obtener fincas: {ex.Message}" });
            }
        }

        [HttpGet("{idFinca:int}/detalle")]
        public async Task<IActionResult> ObtenerDetalle([FromRoute] int idFinca, [FromQuery] int idPropietario)
        {
            try
            {
                if (idFinca <= 0 || idPropietario <= 0) return BadRequest(new { Mensaje = "Parámetros inválidos." });
                var detalle = await _fincaManager.ObtenerDetalleAsync(idFinca, idPropietario);
                return detalle == null ? NotFound(new { Mensaje = "No se encontró la finca solicitada." }) : Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = $"Error al obtener detalle de finca: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarFinca([FromBody] RegistrarFincaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid || dto.IdPropietario <= 0) return ValidationProblem(ModelState);
                var idFinca = await _fincaManager.RegistrarFincaAsync(dto);
                return CreatedAtAction(nameof(ObtenerDetalle), new { idFinca, idPropietario = dto.IdPropietario }, new { IdFinca = idFinca, Mensaje = "Finca registrada correctamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = $"Error inesperado al registrar finca: {ex.Message}" });
            }
        }

        [HttpPut("{idFinca:int}")]
        public async Task<IActionResult> ActualizarFinca([FromRoute] int idFinca, [FromBody] RegistrarFincaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid || idFinca <= 0 || dto.IdPropietario <= 0) return ValidationProblem(ModelState);
                var actualizado = await _fincaManager.ActualizarFincaAsync(idFinca, dto);
                return actualizado ? Ok(new { Mensaje = "Finca actualizada correctamente." }) : NotFound(new { Mensaje = "No fue posible actualizar la finca solicitada." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = $"Error inesperado al actualizar finca: {ex.Message}" });
            }
        }

        [HttpDelete("{idFinca:int}")]
        public async Task<IActionResult> EliminarFinca([FromRoute] int idFinca, [FromQuery] int idPropietario)
        {
            try
            {
                if (idFinca <= 0 || idPropietario <= 0) return BadRequest(new { Mensaje = "Datos inválidos." });
                var eliminado = await _fincaManager.EliminarFincaAsync(idFinca, idPropietario);
                return eliminado ? Ok(new { Mensaje = "Finca eliminada correctamente." }) : NotFound(new { Mensaje = "No se encontró la finca para eliminar." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = $"Error inesperado al eliminar finca: {ex.Message}" });
            }
        }
    }
}
