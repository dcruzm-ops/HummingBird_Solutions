using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class ReportesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.ModuloActivo = "reportes";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Reportes";
            ViewBag.SubtituloPagina = "Consulte reportes operativos, técnicos y administrativos del sistema.";
            ViewBag.BreadcrumbActual = "Reportes";
            return View();
        }
    }
}
