using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.Fincas;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "2")]
    public class FincasController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FincasController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult RegistrarFinca()
        {
            CargarViewBag();
            CargarCatalogosFormularioFinca();
            return View(new RegistrarFincaDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarFinca(RegistrarFincaDTO dto)
        {
            CargarViewBag();
            dto.IdPropietario = ObtenerIdUsuarioSesion();
            if (dto.IdPropietario <= 0) return RedirectToAction("IniciarSesion", "Autenticacion");
            if (!ModelState.IsValid)
            {
                CargarCatalogosFormularioFinca();
                return View(dto);
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PostAsJsonAsync("api/Fincas", dto);
            if (!response.IsSuccessStatusCode)
            {
                TempData["MensajeError"] = "No fue posible registrar la finca.";
                CargarCatalogosFormularioFinca();
                return View(dto);
            }

            TempData["MensajeExito"] = "Finca registrada correctamente.";
            return RedirectToAction(nameof(MisFincas));
        }

        [HttpGet]
        public async Task<IActionResult> MisFincas()
        {
            CargarListadoViewBag();
            var idPropietario = ObtenerIdUsuarioSesion();
            if (idPropietario <= 0) return RedirectToAction("IniciarSesion", "Autenticacion");

            var client = _httpClientFactory.CreateClient("AuthApi");
            var fincas = await client.GetFromJsonAsync<List<FincaResumenDTO>>($"api/Fincas/mis-fincas?idPropietario={idPropietario}")
                ?? new List<FincaResumenDTO>();

            return View(fincas);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleFinca(int? id = null)
        {
            CargarDetalleViewBag();
            var idPropietario = ObtenerIdUsuarioSesion();
            if (idPropietario <= 0) return RedirectToAction("IniciarSesion", "Autenticacion");
            if ((id ?? 0) <= 0) return RedirectToAction(nameof(MisFincas));

            var client = _httpClientFactory.CreateClient("AuthApi");
            var detalle = await client.GetFromJsonAsync<FincaDetalleDTO>($"api/Fincas/{id}/detalle?idPropietario={idPropietario}");
            if (detalle == null)
            {
                TempData["MensajeError"] = "No se encontró la finca solicitada.";
                return RedirectToAction(nameof(MisFincas));
            }

            return View(detalle);
        }

        private int ObtenerIdUsuarioSesion() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private void CargarCatalogosFormularioFinca()
        {
            ViewBag.OpcionesPendiente = new[] { "Baja", "Media", "Alta" };
            ViewBag.OpcionesVegetacion = new[] { "Bosque", "Pasto", "Cultivo" };
            ViewBag.OpcionesUsoSuelo = new[] { "Conservación", "Ganadería", "Agricultura" };
        }

        private void CargarViewBag()
        {
            ViewBag.ModuloActivo = "fincas"; ViewBag.RolActivo = "Dueno"; ViewBag.TituloPagina = "Registrar finca";
            ViewBag.SubtituloPagina = "Complete la información principal de la propiedad para iniciar el proceso.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas"; ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas"); ViewBag.BreadcrumbActual = "Registrar finca";
        }
        private void CargarListadoViewBag()
        {
            ViewBag.ModuloActivo = "fincas"; ViewBag.RolActivo = "Dueno"; ViewBag.TituloPagina = "Mis fincas";
            ViewBag.SubtituloPagina = "Consulte el estado de sus propiedades registradas y sus procesos asociados."; ViewBag.BreadcrumbActual = "Mis fincas";
        }
        private void CargarDetalleViewBag()
        {
            ViewBag.ModuloActivo = "fincas"; ViewBag.RolActivo = "Dueno"; ViewBag.TituloPagina = "Detalle de finca";
            ViewBag.SubtituloPagina = "Visualice la información general, evaluación, evidencias y plan de pago.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas"; ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas"); ViewBag.BreadcrumbActual = "Detalle de finca";
        }
    }
}
