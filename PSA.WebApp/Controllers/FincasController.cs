using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.Fincas;
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
            return View(new FincaDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarFinca(FincaDTO model)
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Registrar finca";
            ViewBag.SubtituloPagina = "Complete la información principal de la propiedad para iniciar el proceso.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas";
            ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas");
            ViewBag.BreadcrumbActual = "Registrar finca";

            model.IdPropietario = ObtenerIdUsuarioSesion();
            model.EstadoFinca = string.IsNullOrWhiteSpace(model.EstadoFinca) ? "Pendiente" : model.EstadoFinca.Trim();

            if (model.IdPropietario <= 0)
            {
                return RedirectToAction("IniciarSesion", "Autenticacion");
            }

            if (string.IsNullOrWhiteSpace(model.Distrito))
            {
                model.Distrito = model.Canton;
            }

            if (string.IsNullOrWhiteSpace(model.UsoSuelo))
            {
                model.UsoSuelo = "Conservación";
            }

            if (string.IsNullOrWhiteSpace(model.Pendiente))
            {
                model.Pendiente = "Media";
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultado = await CrearFincaEnApiConFallbackAsync(model);

            if (!resultado.Exito)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensaje);
                return View(model);
            }

            TempData["MensajeExito"] = "Finca registrada correctamente.";
            return RedirectToAction(nameof(MisFincas));
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

        private async Task<(bool Exito, string Mensaje)> CrearFincaEnApiConFallbackAsync(FincaDTO model)
        {
            try
            {
                var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi")
                    ?? throw new InvalidOperationException("IHttpClientFactory no está disponible.");

                Exception? ultimaExcepcion = null;
                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
                        var response = await client.PostAsJsonAsync($"{baseUrl}/api/Finca/Create", model);
                        if (response.IsSuccessStatusCode)
                        {
                            return (true, string.Empty);
                        }

                        var detalle = await response.Content.ReadAsStringAsync();
                        return (false, $"El API rechazó el registro: {detalle}");
                    }
                    catch (Exception ex)
                    {
                        ultimaExcepcion = ex;
                    }
                }

                if (ultimaExcepcion != null)
                {
                    throw ultimaExcepcion;
                }
            }
            catch
            {
                try
                {
                    _fincaDAO.Create(model);
                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    return (false, $"No fue posible registrar la finca: {ex.Message}");
                }
            }

            return (false, "No fue posible registrar la finca.");
        }

        private async Task<List<FincaResumenDTO>> ObtenerFincasDesdeApiConFallbackAsync(int idPropietario)
        {
            try
            {
                var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi")
                    ?? throw new InvalidOperationException("IHttpClientFactory no está disponible.");

                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
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
                        // Probar siguiente URL
                    }
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

                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
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
                        // Probar siguiente URL
                    }
                }
            }
            catch
            {
                // Fallback local
            }

            return await _fincaDAO.ObtenerDetalleAsync(idFinca, idPropietario);
        }

        private IEnumerable<string> GetApiBaseUrls()
        {
            var configurada = _configuration["ApiSettings:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configurada))
            {
                yield return configurada.TrimEnd('/');
            }

            yield return "https://localhost:59665";
            yield return "http://localhost:59667";
        }

        private int ObtenerIdUsuarioSesion()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var idUsuario) ? idUsuario : 0;
        }
    }
}
