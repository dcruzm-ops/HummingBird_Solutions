using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs.Reportes;

namespace PSA.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var data = await _reportesManager.ObtenerPagosDuenoAsync(idPropietario, new FiltroReporteDTO { Anio = anio, Mes = mes });
        return Ok(data);
    }

    [HttpGet("dueno/{idPropietario:int}/transacciones")]
    public async Task<IActionResult> ObtenerTransaccionesDueno([FromRoute] int idPropietario, [FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        var data = await _reportesManager.ObtenerTransaccionesDuenoAsync(idPropietario, new FiltroReporteDTO { Anio = anio, Mes = mes });
        return Ok(data);
    }

    [HttpGet("ingeniero/{idIngeniero:int}/evaluaciones")]
    public async Task<IActionResult> ObtenerEvaluacionesIngeniero([FromRoute] int idIngeniero, [FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        var data = await _reportesManager.ObtenerEvaluacionesIngenieroAsync(idIngeniero, new FiltroReporteDTO { Anio = anio, Mes = mes });
        return Ok(data);
    }

    [HttpGet("administrador/pagos-ubicacion")]
    public async Task<IActionResult> ObtenerPagosPorUbicacion([FromQuery] int? anio = null, [FromQuery] int? mes = null)
    {
        var data = await _reportesManager.ObtenerPagosPorUbicacionAsync(new FiltroReporteDTO { Anio = anio, Mes = mes });
        return Ok(data);
    }

    [HttpGet("administrador/resumen-actividad")]
    public async Task<IActionResult> ObtenerResumenActividad()
    {
        var data = await _reportesManager.ObtenerResumenActividadAsync();
        return Ok(data);
    }
}
