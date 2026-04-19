using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess.DAO;
using PSA.WebApp.Models;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "2")]
    public class NotificacionesController : Controller
    {
        private readonly DashboardDAO _dashboardDAO;

        public NotificacionesController(DashboardDAO dashboardDAO)
        {
            _dashboardDAO = dashboardDAO;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.ModuloActivo = "notificaciones";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Notificaciones";
            ViewBag.SubtituloPagina = "Revise avisos del sistema asociados a evaluaciones, cuentas y pagos.";
            ViewBag.BreadcrumbActual = "Notificaciones";

            var idUsuario = ObtenerIdUsuarioSesion();
            if (idUsuario <= 0)
            {
                return RedirectToAction("IniciarSesion", "Autenticacion");
            }

            var actividad = await _dashboardDAO.ObtenerActividadDuenoAsync(idUsuario);

            var modelo = actividad
                .Select(x => new ActividadDashboardViewModel
                {
                    Titulo = x.Mensaje,
                    Fecha = x.Fecha
                })
                .ToList();

            return View(modelo);
        }

        private int ObtenerIdUsuarioSesion()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var idUsuario) ? idUsuario : 0;
        }
    }
}