using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FincasController : ControllerBase
    {
        private readonly FincaDAO _fincaDAO;
        private readonly EvaluacionTecnicaManager _evaluacionTecnicaManager;

        public FincasController(FincaDAO fincaDAO, EvaluacionTecnicaManager evaluacionTecnicaManager)
        {
            _fincaDAO = fincaDAO;
            _evaluacionTecnicaManager = evaluacionTecnicaManager;
        }

        [HttpGet("mis-fincas")]
        public async Task<IActionResult> ObtenerMisFincas([FromQuery] int idPropietario = 2)
        {
            if (idPropietario <= 0)
            {
                return BadRequest(new { Mensaje = "El idPropietario debe ser mayor a 0." });
            }

            var fincas = await _fincaDAO.ObtenerPorPropietarioAsync(idPropietario);
            return Ok(fincas);
        }

        [HttpGet("{idFinca:int}/detalle")]
        public async Task<IActionResult> ObtenerDetalle([FromRoute] int idFinca, [FromQuery] int idPropietario = 2)
        {
            if (idFinca <= 0 || idPropietario <= 0)
            {
                return BadRequest(new { Mensaje = "Los parámetros idFinca e idPropietario deben ser mayores a 0." });
            }

            var detalle = await _fincaDAO.ObtenerDetalleAsync(idFinca, idPropietario);
            if (detalle == null)
            {
                return NotFound(new { Mensaje = "No se encontró la finca solicitada." });
            }

            return Ok(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarFinca([FromBody] RegistrarFincaDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (dto.IdPropietario <= 0)
            {
                return BadRequest(new { Mensaje = "El propietario de la finca es inválido." });
            }

            var idFinca = await _fincaDAO.CrearFincaAsync(dto);
            await _evaluacionTecnicaManager.CrearPendientePorNuevaFincaAsync(idFinca);
            return CreatedAtAction(nameof(ObtenerDetalle), new { idFinca, idPropietario = dto.IdPropietario }, new { IdFinca = idFinca, Mensaje = "Finca registrada correctamente." });
        }

        [HttpPut("{idFinca:int}")]
        public async Task<IActionResult> ActualizarFinca([FromRoute] int idFinca, [FromBody] RegistrarFincaDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (idFinca <= 0 || dto.IdPropietario <= 0)
            {
                return BadRequest(new { Mensaje = "Datos de actualización inválidos." });
            }

            var actualizado = await _fincaDAO.ActualizarFincaAsync(idFinca, dto);
            if (!actualizado)
            {
                return NotFound(new { Mensaje = "No fue posible actualizar la finca solicitada." });
            }

            return Ok(new { Mensaje = "Finca actualizada correctamente." });
        }

        [HttpDelete("{idFinca:int}")]
        public async Task<IActionResult> EliminarFinca([FromRoute] int idFinca, [FromQuery] int idPropietario)
        {
            if (idFinca <= 0 || idPropietario <= 0)
            {
                return BadRequest(new { Mensaje = "Datos inválidos para eliminar la finca." });
            }

            var eliminado = await _fincaDAO.EliminarFincaAsync(idFinca, idPropietario);
            if (!eliminado)
            {
                return NotFound(new { Mensaje = "No se encontró la finca para eliminar." });
            }

            return Ok(new { Mensaje = "Finca eliminada correctamente." });
        }
    }
}
