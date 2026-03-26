using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class FincasController : Controller
    {
        [HttpGet]
        public IActionResult RegistrarFinca()
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Registrar finca";
            ViewBag.SubtituloPagina = "Complete la información principal de la propiedad para iniciar el proceso.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas";
            ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas");
            ViewBag.BreadcrumbActual = "Registrar finca";
            return View();
        }

        [HttpGet]
        public IActionResult MisFincas()
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Mis fincas";
            ViewBag.SubtituloPagina = "Consulte el estado de sus propiedades registradas y sus procesos asociados.";
            ViewBag.BreadcrumbActual = "Mis fincas";
            return View();
        }

        [HttpGet]
        public IActionResult DetalleFinca(int? id = null)
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.FincaId = id ?? 1;
            ViewBag.TituloPagina = "Detalle de finca";
            ViewBag.SubtituloPagina = "Visualice la información general, evaluación, evidencias y plan de pago.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas";
            ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas");
            ViewBag.BreadcrumbActual = "Detalle de finca";
            return View();
        }
    }
}
