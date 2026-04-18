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
}
