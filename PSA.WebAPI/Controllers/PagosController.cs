using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagosController(PagosManager pagosManager) : BaseApiController
{
    private readonly PagosManager _pagosManager = pagosManager;

    [HttpPost("generar-plan")]
    public async Task<ActionResult<PlanPagoDTO>> GenerarPlan([FromBody] GenerarPlanPagoRequestDTO request)
    {
        try
        {
            var plan = await _pagosManager.GenerarPlanPagoAsync(
                request,
                AdminSistemaId,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (plan == null)
            {
                return NotFound(new { Mensaje = "No fue posible generar el plan con los datos actuales." });
            }

            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpGet("dueno/{idPropietario:int}/historial")]
    public async Task<ActionResult<List<CuotaPlanPagoDTO>>> ObtenerHistorialDueno([FromRoute] int idPropietario)
    {
        try
        {
            var historial = await _pagosManager.ObtenerHistorialDuenoAsync(idPropietario);
            return Ok(historial);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpGet("dueno/{idPropietario:int}/planes")]
    public async Task<ActionResult<List<PlanPagoResumenDTO>>> ObtenerPlanesDueno([FromRoute] int idPropietario)
    {
        try
        {
            var planes = await _pagosManager.ObtenerPlanesDuenoAsync(idPropietario);
            return Ok(planes);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpGet("ingeniero/{idIngeniero:int}/planes-pendientes")]
    public async Task<ActionResult<List<PlanPagoResumenDTO>>> ObtenerPlanesPendientesIngeniero([FromRoute] int idIngeniero)
    {
        try
        {
            var planes = await _pagosManager.ObtenerPlanesPendientesIngenieroAsync(idIngeniero);
            return Ok(planes);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpGet("planes")]
    public async Task<ActionResult<List<PlanPagoResumenDTO>>> ObtenerPlanesConFiltros(
        [FromQuery] int? anio = null,
        [FromQuery] int? idFinca = null,
        [FromQuery] int? idPropietario = null,
        [FromQuery] int? idIngeniero = null,
        [FromQuery] string? estadoPlan = null,
        [FromQuery] bool soloPendientes = false)
    {
        try
        {
            var planes = await _pagosManager.ObtenerPlanesConFiltrosAsync(new FiltroPlanesPagoDTO
            {
                Anio = anio,
                IdFinca = idFinca,
                IdPropietario = idPropietario,
                IdIngeniero = idIngeniero,
                EstadoPlan = estadoPlan,
                SoloPendientes = soloPendientes
            });

            return Ok(planes);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpGet("dueno/{idUsuario:int}/cuentas-bancarias")]
    public async Task<ActionResult<List<CuentaBancariaDuenoDTO>>> ObtenerCuentasBancariasDueno([FromRoute] int idUsuario)
    {
        try
        {
            var cuentas = await _pagosManager.ObtenerCuentasBancariasDuenoAsync(idUsuario);
            return Ok(cuentas);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpPost("dueno/cuentas-bancarias")]
    public async Task<ActionResult<int>> RegistrarCuentaBancariaDueno([FromBody] RegistrarCuentaBancariaDTO model)
    {
        try
        {
            var idCuenta = await _pagosManager.RegistrarCuentaBancariaDuenoAsync(model);
            return Ok(idCuenta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpPut("dueno/planes/{idPlanPago:int}/cuenta-bancaria")]
    public async Task<IActionResult> AsociarCuentaPlan([FromRoute] int idPlanPago, [FromBody] AsociarCuentaPlanDTO model)
    {
        try
        {
            var actualizado = await _pagosManager.AsociarCuentaPlanAsync(idPlanPago, model, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!actualizado)
            {
                return BadRequest(new { Mensaje = "No fue posible asociar la cuenta al plan seleccionado." });
            }

            return Ok(new { Mensaje = "Cuenta bancaria asociada correctamente al plan de pago. Estado: PendienteAprobacionFinal." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpPut("ingeniero/planes/{idPlanPago:int}/aprobar-final")]
    public async Task<IActionResult> AprobarPlanFinal([FromRoute] int idPlanPago, [FromBody] AprobarPlanPagoFinalDTO model)
    {
        try
        {
            var aprobado = await _pagosManager.AprobarPlanFinalAsync(idPlanPago, model, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!aprobado)
            {
                return BadRequest(new { Mensaje = "No fue posible activar el plan. Verifique estado pendiente de aprobación y cuenta bancaria válida." });
            }

            return Ok(new { Mensaje = "Plan de pago activado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }
}
