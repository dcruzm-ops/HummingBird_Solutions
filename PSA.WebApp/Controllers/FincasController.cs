using Microsoft.AspNetCore.Mvc;
using PSA.WebApp.Models.Fincas;
using PSA.WebApp.Services;

namespace PSA.WebApp.Controllers
{
    public class FincasController : Controller
    {
        private readonly FincaApiService _fincaApiService;

        public FincasController(FincaApiService fincaApiService)
        {
            _fincaApiService = fincaApiService;
        }

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

            return View(new FincaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarFinca(FincaViewModel model)
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Registrar finca";
            ViewBag.SubtituloPagina = "Complete la información principal de la propiedad para iniciar el proceso.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas";
            ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas");
            ViewBag.BreadcrumbActual = "Registrar finca";

            model.FechaRegistro = DateTime.Now;
            model.FechaActualizacion = DateTime.Now;

            var result = await _fincaApiService.CreateAsync(model);

            if (!result.Ok)
            {
                ViewBag.Error = $"No se pudo guardar la finca. Detalle: {result.Error}";
                return View(model);
            }

            TempData["Success"] = "Finca guardada correctamente.";
            return RedirectToAction("MisFincas");
        }

        [HttpGet]
        public async Task<IActionResult> MisFincas()
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Mis fincas";
            ViewBag.SubtituloPagina = "Consulte el estado de sus propiedades registradas y sus procesos asociados.";
            ViewBag.BreadcrumbActual = "Mis fincas";

            var fincas = await _fincaApiService.RetrieveAllAsync();
            return View(fincas);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleFinca(int id)
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Detalle de finca";
            ViewBag.SubtituloPagina = "Visualice la información general, evaluación, evidencias y plan de pago.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas";
            ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas");
            ViewBag.BreadcrumbActual = "Detalle de finca";

            var finca = await _fincaApiService.RetrieveByIdAsync(id);

            if (finca == null)
                return RedirectToAction("MisFincas");

            return View(finca);
        }
    }
}