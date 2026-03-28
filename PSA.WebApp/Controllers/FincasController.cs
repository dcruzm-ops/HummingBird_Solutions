using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using System.Net.Http.Json;

namespace PSA.WebApp.Controllers
{
    [Authorize]
    public class FincasController : Controller
    {
        private readonly FincaDAO _fincaDAO;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public FincasController(
            FincaDAO fincaDAO,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _fincaDAO = fincaDAO;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
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

            var idPropietario = ObtenerIdUsuarioSesion();
            if (idPropietario <= 0)
            {
                return RedirectToAction("IniciarSesion", "Autenticacion");
            }

            var fincas = await ObtenerFincasDesdeApiConFallbackAsync(idPropietario);
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

            var idPropietario = ObtenerIdUsuarioSesion();
            if (idPropietario <= 0)
            {
                return RedirectToAction("IniciarSesion", "Autenticacion");
            }

            var detalle = await ObtenerDetalleDesdeApiConFallbackAsync(idFinca, idPropietario);
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
                var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi")
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
                var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi")
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

        private int ObtenerIdUsuarioSesion()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var idUsuario) ? idUsuario : 0;
        }
    }
}
