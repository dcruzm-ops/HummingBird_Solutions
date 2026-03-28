using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using System.Net.Http.Json;

namespace PSA.WebApp.Controllers
{
    public class FincasController : Controller
    {
        private readonly FincaDAO _fincaDAO;
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly IConfiguration _configuration;
        private const int IdPropietarioDemo = 2;

        public FincasController(
            FincaDAO fincaDAO,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _fincaDAO = fincaDAO;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public FincasController(
            FincaDAO fincaDAO,
            IConfiguration configuration)
        {
            _fincaDAO = fincaDAO;
            _configuration = configuration;
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
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MisFincas()
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Mis fincas";
            ViewBag.SubtituloPagina = "Consulte el estado de sus propiedades registradas y sus procesos asociados.";
            ViewBag.BreadcrumbActual = "Mis fincas";

            var fincas = await ObtenerFincasDesdeApiConFallbackAsync(IdPropietarioDemo);
            return View(fincas);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleFinca(int? id = null)
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Detalle de finca";
            ViewBag.SubtituloPagina = "Visualice la información general, evaluación, evidencias y plan de pago.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas";
            ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas");
            ViewBag.BreadcrumbActual = "Detalle de finca";

            var idFinca = id ?? 0;
            if (idFinca <= 0)
            {
                return RedirectToAction(nameof(MisFincas));
            }

            var detalle = await ObtenerDetalleDesdeApiConFallbackAsync(idFinca, IdPropietarioDemo);
            if (detalle == null)
            {
                TempData["MensajeError"] = "No se encontró la finca solicitada para el propietario actual.";
                return RedirectToAction(nameof(MisFincas));
            }

            return View(detalle);
        }

        private async Task<List<FincaResumenDTO>> ObtenerFincasDesdeApiConFallbackAsync(int idPropietario)
        {
            try
            {
                var client = _httpClientFactory?.CreateClient("AuthApi")
                    ?? throw new InvalidOperationException("IHttpClientFactory no está disponible.");
                var baseUrl = GetApiBaseUrl();
                var fincas = await client.GetFromJsonAsync<List<FincaResumenDTO>>(
                    $"{baseUrl}/api/Fincas/mis-fincas?idPropietario={idPropietario}"
                );

                if (fincas != null)
                {
                    return fincas;
                }
            }
            catch
            {
                // Fallback local
            }

            return await _fincaDAO.ObtenerPorPropietarioAsync(idPropietario);
        }

        private async Task<FincaDetalleDTO?> ObtenerDetalleDesdeApiConFallbackAsync(int idFinca, int idPropietario)
        {
            try
            {
                var client = _httpClientFactory?.CreateClient("AuthApi")
                    ?? throw new InvalidOperationException("IHttpClientFactory no está disponible.");
                var baseUrl = GetApiBaseUrl();
                var detalle = await client.GetFromJsonAsync<FincaDetalleDTO>(
                    $"{baseUrl}/api/Fincas/{idFinca}/detalle?idPropietario={idPropietario}"
                );

                if (detalle != null)
                {
                    return detalle;
                }
            }
            catch
            {
                // Fallback local
            }

            return await _fincaDAO.ObtenerDetalleAsync(idFinca, idPropietario);
        }

        private string GetApiBaseUrl()
        {
            return (_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:59665").TrimEnd('/');
        }
    }
}
