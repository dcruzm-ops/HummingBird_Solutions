using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Reportes;
using PSA.WebAPI.Extensions;

namespace PSA.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly ReportesManager _reportesManager;

    public ReportesController(ReportesManager reportesManager)
    {
        _reportesManager = reportesManager;
    }

    [HttpGet("dueno/{idPropietario:int}/pagos")]
    public async Task<IActionResult> ObtenerPagosDueno([FromRoute] int idPropietario, [FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        var target = IsRole("1") ? idPropietario : GetUserId();
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerPagosDuenoAsync(target, new FiltroReporteDTO { Anio = anio, Mes = mes }));
    }

    [HttpGet("dueno/{idPropietario:int}/transacciones")]
    public async Task<IActionResult> ObtenerTransaccionesDueno([FromRoute] int idPropietario, [FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        var target = IsRole("1") ? idPropietario : GetUserId();
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerTransaccionesDuenoAsync(target, new FiltroReporteDTO { Anio = anio, Mes = mes }));
    }

    [HttpGet("ingeniero/{idIngeniero:int}/evaluaciones")]
    public async Task<IActionResult> ObtenerEvaluacionesIngeniero([FromRoute] int idIngeniero, [FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        var target = IsRole("1") ? idIngeniero : GetUserId();
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerEvaluacionesIngenieroAsync(target, new FiltroReporteDTO { Anio = anio, Mes = mes }));
    }

    [HttpGet("administrador/pagos-ubicacion")]
    public async Task<IActionResult> ObtenerPagosPorUbicacion([FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerPagosPorUbicacionAsync(new FiltroReporteDTO { Anio = anio, Mes = mes }));
    }

    [HttpGet("administrador/resumen-actividad")]
    public async Task<IActionResult> ObtenerResumenActividad()
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerResumenActividadAsync());
    }

    [HttpGet("dueno/{idPropietario:int}/fincas")]
    public async Task<IActionResult> ObtenerFincasDueno([FromRoute] int idPropietario)
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerReporteFincasDuenoAsync(idPropietario));
    }

    [HttpGet("ingeniero/fincas-pendientes")]
    public async Task<IActionResult> ObtenerFincasPendientesIngeniero()
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerFincasPendientesIngenieroAsync());
    }

    [HttpGet("ingeniero/{idIngeniero:int}/tecnico-finca")]
    public async Task<IActionResult> ObtenerReporteTecnicoFinca([FromRoute] int idIngeniero, [FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        var target = IsRole("1") ? idIngeniero : GetUserId();
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerReporteTecnicoPorFincaAsync(target, new FiltroReporteDTO { Anio = anio, Mes = mes }));
    }

    [HttpGet("administrador/usuarios-roles")]
    public async Task<IActionResult> ObtenerUsuariosRoles()
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerReporteUsuariosRolesAsync());
    }

    [HttpGet("administrador/fincas-estado")]
    public async Task<IActionResult> ObtenerFincasPorEstado()
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerReporteFincasPorEstadoAsync());
    }

    [HttpGet("administrador/evaluaciones-tecnicas")]
    public async Task<IActionResult> ObtenerEvaluacionesTecnicasAdmin([FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerReporteEvaluacionesAdminAsync(new FiltroReporteDTO { Anio = anio, Mes = mes }));
    }

    [HttpGet("administrador/pagos")]
    public async Task<IActionResult> ObtenerPagosAdmin([FromQuery] int? anio = null)
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerReportePagosAdminAsync(anio));
    }

    [HttpGet("administrador/auditoria-critica")]
    public async Task<IActionResult> ObtenerAuditoriaCritica([FromQuery] int top = 50)
    {
        return await EjecutarSeguro(async () => await _reportesManager.ObtenerAuditoriaCriticaAsync(top));
    }

    private async Task<IActionResult> EjecutarSeguro<T>(Func<Task<T>> accion)
    {
        try
        {
            var data = await accion();
            return Ok(data);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Mensaje = "Ocurrió un error inesperado al generar el reporte." });
        }
    }
}
