using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class NotificacionesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.ModuloActivo = "notificaciones";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Notificaciones";
            ViewBag.SubtituloPagina = "Revise avisos del sistema asociados a evaluaciones, cuentas y pagos.";
            ViewBag.BreadcrumbActual = "Notificaciones";
            return View();
        }
    }
}
