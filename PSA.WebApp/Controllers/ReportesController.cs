using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "1,2,3")]
    public class ReportesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.ModuloActivo = "reportes";
            ViewBag.RolActivo = ObtenerNombreRol();
            ViewBag.TituloPagina = "Reportes";
            ViewBag.SubtituloPagina = "Consulte reportes base según su rol dentro del sistema.";
            ViewBag.BreadcrumbActual = "Reportes";
            return View();
        }

        private string ObtenerNombreRol()
        {
            var rolId = User.FindFirstValue(ClaimTypes.Role);
            return rolId switch
            {
                "2" => "Administrador",
                "3" => "Ingeniero",
                _ => "Dueno"
            };
        }
    }
}
