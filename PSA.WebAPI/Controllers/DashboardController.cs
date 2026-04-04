using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Dashboard;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardDAO _dashboardDao;

        public DashboardController(DashboardDAO dashboardDao)
        {
            _dashboardDao = dashboardDao;
        }

        [HttpGet("dueno-resumen/{idUsuario:int}")]
        public async Task<IActionResult> ObtenerResumenDuenoAsync(int idUsuario)
        {
            if (idUsuario <= 0) return BadRequest(new { Mensaje = "Id inválido." });
            var resumen = await _dashboardDao.ObtenerResumenDuenoAsync(idUsuario);
            return Ok(new
            {
                resumen.FincasRegistradas,
                resumen.EvaluacionesPendientes,
                resumen.CuotasPorConfirmar,
                Actividad = resumen.Actividad.Select(x => new { x.Mensaje, x.IdEntidad })
            });
        }

        [HttpGet("ingeniero-resumen/{idUsuario:int}")]
        public async Task<IActionResult> ObtenerResumenIngenieroAsync(int idUsuario)
        {
            if (idUsuario <= 0) return BadRequest(new { Mensaje = "Id inválido." });
            var resumen = await _dashboardDao.ObtenerResumenIngenieroAsync(idUsuario);
            return Ok(new
            {
                resumen.FincasPendientes,
                resumen.EvaluacionesAbiertas,
                resumen.DecisionesMesActual,
                ProximasAcciones = resumen.ProximasAcciones.Select(x => new { x.IdFinca, x.NombreFinca })
            });
        }

        [HttpGet("administrador-resumen")]
        public async Task<ActionResult<ResumenDashboardAdministradorDTO>> ObtenerResumenAdministradorAsync()
        {
            var resumen = await _dashboardDao.ObtenerResumenAdministradorAsync();
            return Ok(new ResumenDashboardAdministradorDTO
            {
                UsuariosActivos = resumen.UsuariosActivos,
                CuentasPorValidar = resumen.CuentasPorValidar,
                EventosAuditoria24h = resumen.EventosAuditoria24h,
                Alertas = resumen.Alertas
            });
        }
    }
}
