using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagosController(PagosManager pagosManager) : ControllerBase
{
    private readonly PagosManager _pagosManager = pagosManager;

    [HttpPost("generar-plan")]
    public async Task<ActionResult<PlanPagoDTO>> GenerarPlan([FromBody] GenerarPlanPagoRequestDTO request)
    {
        try
        {
            var plan = await _pagosManager.GenerarPlanPagoAsync(
                request,
                idUsuario: 1,
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

    [HttpGet("dueno/{idPropietario:int}/planes")]
    public async Task<ActionResult<List<OwnerPaymentPlanDto>>> ObtenerPlanesDueno([FromRoute] int idPropietario)
    {
        try
        {
            return Ok(await _pagosManager.ObtenerPlanesOwnerAsync(idPropietario));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpGet("dueno/{idPropietario:int}/planes/{idPlanPago:int}")]
    public async Task<ActionResult<OwnerPaymentPlanDetailDto>> ObtenerDetalleDueno([FromRoute] int idPropietario, [FromRoute] int idPlanPago)
    {
        try
        {
            var detalle = await _pagosManager.ObtenerDetalleOwnerAsync(idPropietario, idPlanPago);
            if (detalle == null)
            {
                return NotFound(new { Mensaje = "No se encontró el plan solicitado para el propietario." });
            }

            return Ok(detalle);
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

    [HttpGet("ingeniero/{idIngeniero:int}/evaluaciones/{idEvaluacion:int}/impacto")]
    public async Task<ActionResult<EngineerPaymentImpactDto>> ObtenerImpactoIngeniero([FromRoute] int idIngeniero, [FromRoute] int idEvaluacion)
    {
        try
        {
            var impacto = await _pagosManager.ObtenerImpactoIngenieroAsync(idIngeniero, idEvaluacion);
            if (impacto == null)
            {
                return NotFound(new { Mensaje = "No se encontró la evaluación o no pertenece al ingeniero." });
            }

            return Ok(impacto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpGet("admin/planes")]
    public async Task<ActionResult<List<AdminPaymentPlanDto>>> ObtenerPlanesAdmin(
        [FromQuery] int? anio = null,
        [FromQuery] int? idFinca = null,
        [FromQuery] int? idPropietario = null,
        [FromQuery] int? idIngeniero = null,
        [FromQuery] string? provincia = null,
        [FromQuery] string? canton = null,
        [FromQuery] string? distrito = null,
        [FromQuery] string? estadoPlan = null,
        [FromQuery] string? estadoBancario = null)
    {
        try
        {
            var filtro = new AdminPaymentPlanFilterDto
            {
                Anio = anio,
                IdFinca = idFinca,
                IdPropietario = idPropietario,
                IdIngeniero = idIngeniero,
                Provincia = provincia,
                Canton = canton,
                Distrito = distrito,
                EstadoPlan = estadoPlan,
                EstadoBancario = estadoBancario
            };

            return Ok(await _pagosManager.ObtenerPlanesAdminAsync(filtro));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpGet("admin/planes/{idPlanPago:int}")]
    public async Task<ActionResult<AdminPaymentPlanDetailDto>> ObtenerDetalleAdmin([FromRoute] int idPlanPago)
    {
        try
        {
            var detalle = await _pagosManager.ObtenerDetalleAdminAsync(idPlanPago);
            if (detalle == null)
            {
                return NotFound(new { Mensaje = "No se encontró el plan solicitado." });
            }

            return Ok(detalle);
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
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Mensaje = "No fue posible registrar la cuenta bancaria en este momento." });
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

            return Ok(new { Mensaje = "Cuenta bancaria asociada correctamente al plan de pago." });
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
                return BadRequest(new { Mensaje = "No fue posible activar el plan. Verifique estado y cuenta bancaria válida." });
            }

            return Ok(new { Mensaje = "Plan de pago activado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }
}
