using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess.DAO;
using PSA.WebApp.Models;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DashboardDAO _dashboardDAO;

        public DashboardController(DashboardDAO dashboardDAO)
        {
            _dashboardDAO = dashboardDAO;
        }

        [HttpGet]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> Dueno()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Dashboard del dueño de finca";
            ViewBag.SubtituloPagina = "Resumen general de fincas, evaluaciones, notificaciones y pagos.";
            ViewBag.BreadcrumbActual = "Dashboard";

            var idUsuario = ObtenerIdUsuarioSesion();
            if (idUsuario <= 0)
            {
                return RedirectToAction("IniciarSesion", "Autenticacion");
            }

            var resumen = await _dashboardDAO.ObtenerResumenDuenoAsync(idUsuario);

            var modelo = new DashboardDuenoViewModel
            {
                FincasRegistradas = resumen.FincasRegistradas,
                EvaluacionesPendientes = resumen.EvaluacionesPendientes,
                CuotasPorConfirmar = resumen.CuotasPorConfirmar,
                ActividadReciente = resumen.Actividad
                    .Select(x => new ActividadDashboardViewModel
                    {
                        Titulo = x.Mensaje,
                        Fecha = x.Fecha,
                        Url = null
                    })
                    .ToList()
            };

            return View(modelo);
        }

        [HttpGet]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Ingeniero()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.TituloPagina = "Dashboard del ingeniero forestal";
            ViewBag.SubtituloPagina = "Accesos rápidos a evaluaciones, visitas y fincas pendientes.";
            ViewBag.BreadcrumbActual = "Dashboard";

            var idUsuario = ObtenerIdUsuarioSesion();
            if (idUsuario <= 0)
            {
                return RedirectToAction("IniciarSesion", "Autenticacion");
            }

            var resumen = await _dashboardDAO.ObtenerResumenIngenieroAsync(idUsuario);

            var modelo = new DashboardIngenieroViewModel
            {
                FincasPendientes = resumen.FincasPendientes,
                EvaluacionesAbiertas = resumen.EvaluacionesAbiertas,
                DecisionesMesActual = resumen.DecisionesMesActual,
                ColaPendientesVisita = new(),
                ProximasAcciones = resumen.ProximasAcciones
                    .Select(x => new ActividadDashboardViewModel
                    {
                        Titulo = $"Registrar o completar evaluación para \"{x.NombreFinca}\".",
                        Fecha = DateTime.Now,
                        Url = Url.Action("NuevaEvaluacion", "Evaluaciones", new { fincaId = x.IdFinca })
                    })
                    .ToList()
            };

            return View(modelo);
        }

        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> Administrador()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Dashboard del administrador";
            ViewBag.SubtituloPagina = "Monitoreo operativo del sistema, usuarios, pagos y auditoría.";
            ViewBag.BreadcrumbActual = "Dashboard";

            var resumen = await _dashboardDAO.ObtenerResumenAdministradorAsync();

            var modelo = new DashboardAdministradorViewModel
            {
                UsuariosActivos = resumen.UsuariosActivos,
                CuentasPorValidar = resumen.CuentasPorValidar,
                EventosAuditoria24h = resumen.EventosAuditoria24h,
                Alertas = resumen.Alertas
                    .Select(x => new ActividadDashboardViewModel
                    {
                        Titulo = x,
                        Fecha = DateTime.Now,
                        Url = null
                    })
                    .ToList()
            };

            return View(modelo);
        }

        private int ObtenerIdUsuarioSesion()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var idUsuario) ? idUsuario : 0;
        }
    }
}