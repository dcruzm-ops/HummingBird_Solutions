using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Pagos;
using PSA.WebAPI.Extensions;

namespace PSA.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagosController(PagosManager pagosManager) : ControllerBase
{
    private readonly PagosManager _pagosManager = pagosManager;

    [HttpPost("generar-plan")]
    [Authorize(Roles = "3")]
    public async Task<ActionResult<PlanPagoDTO>> GenerarPlan([FromBody] GenerarPlanPagoRequestDTO request)
    {
        try
        {
            var plan = await _pagosManager.GenerarPlanPagoAsync(request, this.GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
            return plan == null ? NotFound(new { Mensaje = "No fue posible generar el plan con los datos actuales." }) : Ok(plan);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { Mensaje = ex.Message }); }
    }

    [HttpGet("dueno/{idPropietario:int}/planes")]
    [Authorize(Roles = "2")]
    public async Task<ActionResult<List<OwnerPaymentPlanDto>>> ObtenerPlanesDueno([FromRoute] int idPropietario)
        => Ok(await _pagosManager.ObtenerPlanesOwnerAsync(this.GetUserId()));

    [HttpGet("dueno/{idPropietario:int}/planes/{idPlanPago:int}")]
    [Authorize(Roles = "2")]
    public async Task<ActionResult<OwnerPaymentPlanDetailDto>> ObtenerDetalleDueno([FromRoute] int idPropietario, [FromRoute] int idPlanPago)
    {
        var detalle = await _pagosManager.ObtenerDetalleOwnerAsync(this.GetUserId(), idPlanPago);
        return detalle == null ? NotFound(new { Mensaje = "No se encontró el plan solicitado para el propietario." }) : Ok(detalle);
    }

    [HttpGet("dueno/{idPropietario:int}/historial")]
    [Authorize(Roles = "2")]
    public async Task<ActionResult<List<CuotaPlanPagoDTO>>> ObtenerHistorialDueno([FromRoute] int idPropietario)
        => Ok(await _pagosManager.ObtenerHistorialDuenoAsync(this.GetUserId()));

    [HttpGet("ingeniero/{idIngeniero:int}/evaluaciones/{idEvaluacion:int}/impacto")]
    [Authorize(Roles = "3")]
    public async Task<ActionResult<EngineerPaymentImpactDto>> ObtenerImpactoIngeniero([FromRoute] int idIngeniero, [FromRoute] int idEvaluacion)
    {
        var impacto = await _pagosManager.ObtenerImpactoIngenieroAsync(this.GetUserId(), idEvaluacion);
        return impacto == null ? NotFound(new { Mensaje = "No se encontró la evaluación o no pertenece al ingeniero." }) : Ok(impacto);
    }

    [HttpGet("admin/planes")]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<List<AdminPaymentPlanDto>>> ObtenerPlanesAdmin([FromQuery] int? anio = null, [FromQuery] int? idFinca = null, [FromQuery] int? idPropietario = null, [FromQuery] int? idIngeniero = null, [FromQuery] string? provincia = null, [FromQuery] string? canton = null, [FromQuery] string? distrito = null, [FromQuery] string? estadoPlan = null, [FromQuery] string? estadoBancario = null)
        => Ok(await _pagosManager.ObtenerPlanesAdminAsync(new AdminPaymentPlanFilterDto { Anio = anio, IdFinca = idFinca, IdPropietario = idPropietario, IdIngeniero = idIngeniero, Provincia = provincia, Canton = canton, Distrito = distrito, EstadoPlan = estadoPlan, EstadoBancario = estadoBancario }));

    [HttpGet("admin/planes/{idPlanPago:int}")]
    [Authorize(Roles = "1")]
    public async Task<ActionResult<AdminPaymentPlanDetailDto>> ObtenerDetalleAdmin([FromRoute] int idPlanPago)
    {
        var detalle = await _pagosManager.ObtenerDetalleAdminAsync(idPlanPago);
        return detalle == null ? NotFound(new { Mensaje = "No se encontró el plan solicitado." }) : Ok(detalle);
    }

    [HttpGet("dueno/{idUsuario:int}/cuentas-bancarias")]
    [Authorize(Roles = "2")]
    public async Task<ActionResult<List<CuentaBancariaDuenoDTO>>> ObtenerCuentasBancariasDueno([FromRoute] int idUsuario)
        => Ok(await _pagosManager.ObtenerCuentasBancariasDuenoAsync(this.GetUserId()));

    [HttpPost("dueno/cuentas-bancarias")]
    [Authorize(Roles = "2")]
    public async Task<ActionResult<int>> RegistrarCuentaBancariaDueno([FromBody] RegistrarCuentaBancariaDTO model)
    {
        model.IdUsuario = this.GetUserId();
        var idCuenta = await _pagosManager.RegistrarCuentaBancariaDuenoAsync(model);
        return Ok(idCuenta);
    }

    [HttpPut("dueno/planes/{idPlanPago:int}/cuenta-bancaria")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> AsociarCuentaPlan([FromRoute] int idPlanPago, [FromBody] AsociarCuentaPlanDTO model)
    {
        model.IdUsuario = this.GetUserId();
        var actualizado = await _pagosManager.AsociarCuentaPlanAsync(idPlanPago, model, HttpContext.Connection.RemoteIpAddress?.ToString());
        return actualizado ? Ok(new { Mensaje = "Cuenta bancaria asociada correctamente al plan de pago." }) : BadRequest(new { Mensaje = "No fue posible asociar la cuenta al plan seleccionado." });
    }

    [HttpPost("admin/arrastrar-saldos")]
    [Authorize(Roles = "1")]
    [Authorize(Policy = Services.Security.AppPermissions.AdminPagosConfigurar)]
    public async Task<IActionResult> ArrastrarSaldosPendientes()
    {
        var total = await _pagosManager.ArrastrarSaldosPendientesAsync(this.GetUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(new { Mensaje = "Arrastre de saldos ejecutado.", CuotasAfectadas = total });
    }

    [HttpPut("ingeniero/planes/{idPlanPago:int}/aprobar-final")]
    [Authorize(Roles = "3")]
    [Authorize(Policy = Services.Security.AppPermissions.IngenieroAprobarPlan)]
    public async Task<IActionResult> AprobarPlanFinal([FromRoute] int idPlanPago, [FromBody] AprobarPlanPagoFinalDTO model)
    {
        model.IdIngeniero = this.GetUserId();
        var aprobado = await _pagosManager.AprobarPlanFinalAsync(idPlanPago, model, HttpContext.Connection.RemoteIpAddress?.ToString());
        return aprobado ? Ok(new { Mensaje = "Plan de pago activado correctamente." }) : BadRequest(new { Mensaje = "No fue posible activar el plan. Verifique estado y cuenta bancaria válida." });
    }
}
