using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.WebAPI.Extensions;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FincasController : ControllerBase
    {
        private readonly FincaManager _fincaManager;

        public FincasController(FincaManager fincaManager)
        {
            _fincaManager = fincaManager;
        }

        [HttpGet("mis-fincas")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> ObtenerMisFincas() => Ok(await _fincaManager.ObtenerPorPropietarioAsync(this.GetUserId()));

        [HttpGet("{idFinca:int}/detalle")]
        [Authorize(Roles = "2,3")]
        public async Task<IActionResult> ObtenerDetalle([FromRoute] int idFinca)
        {
            var detalle = await _fincaManager.ObtenerDetalleAsync(idFinca, this.GetUserId());
            return detalle == null ? NotFound(new { Mensaje = "No se encontró la finca solicitada." }) : Ok(detalle);
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> RegistrarFinca([FromBody] PSA.EntidadesDTO.DTOs.RegistrarFincaDTO dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            dto.IdPropietario = this.GetUserId();
            var idFinca = await _fincaManager.RegistrarFincaAsync(dto);
            return CreatedAtAction(nameof(ObtenerDetalle), new { idFinca }, new { IdFinca = idFinca, Mensaje = "Finca registrada correctamente." });
        }

        [HttpPost("{idFinca:int}/renovacion-anual")]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> RenovacionAnual([FromRoute] int idFinca)
        {
            var idEvaluacion = await _fincaManager.GenerarRenovacionAnualAsync(idFinca, this.GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new { IdEvaluacion = idEvaluacion, Mensaje = "Se generó la renovación anual y quedó en cola de pendientes." });
        }
    }
}
