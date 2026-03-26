using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class DashboardController : Controller
    {
        [HttpGet]
        public IActionResult Dueno()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Dashboard del dueño de finca";
            ViewBag.SubtituloPagina = "Resumen general de fincas, evaluaciones, notificaciones y pagos.";
            ViewBag.BreadcrumbActual = "Dashboard";
            return View();
        }

        [HttpGet]
        public IActionResult Ingeniero()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.TituloPagina = "Dashboard del ingeniero forestal";
            ViewBag.SubtituloPagina = "Accesos rápidos a evaluaciones, visitas y fincas pendientes.";
            ViewBag.BreadcrumbActual = "Dashboard";
            return View();
        }

        [HttpGet]
        public IActionResult Administrador()
        {
            ViewBag.ModuloActivo = "dashboard";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Dashboard del administrador";
            ViewBag.SubtituloPagina = "Monitoreo operativo del sistema, usuarios, pagos y auditoría.";
            ViewBag.BreadcrumbActual = "Dashboard";
            return View();
        }
    }
}
