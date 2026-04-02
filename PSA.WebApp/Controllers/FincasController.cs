using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.Fincas;
using System.Net.Http.Json;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "2")]
    public class FincasController : Controller
    {
        private readonly FincaDAO _fincaDAO;
        private readonly EvaluacionTecnicaDAO _evaluacionTecnicaDAO;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public FincasController(
            FincaDAO fincaDAO,
            EvaluacionTecnicaDAO evaluacionTecnicaDAO,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _fincaDAO = fincaDAO;
            _evaluacionTecnicaDAO = evaluacionTecnicaDAO;
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

            CargarCatalogosFormularioFinca();
            return View(new RegistrarFincaDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarFinca(RegistrarFincaDTO dto)
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Registrar finca";
            ViewBag.SubtituloPagina = "Complete la información principal de la propiedad para iniciar el proceso.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas";
            ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas");
            ViewBag.BreadcrumbActual = "Registrar finca";

            dto.IdPropietario = ObtenerIdUsuarioSesion();
            if (dto.IdPropietario <= 0)
            {
                TempData["MensajeError"] = "Debe iniciar sesión para registrar una finca.";
                return RedirectToAction("IniciarSesion", "Autenticacion");
            }

            if (!ModelState.IsValid)
            {
                CargarCatalogosFormularioFinca();
                return View(dto);
            }

            var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi");
            if (client != null)
            {
                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
                        var response = await client.PostAsJsonAsync($"{baseUrl}/api/Fincas", dto);
                        if (!response.IsSuccessStatusCode)
                        {
                            continue;
                        }

                        var idFincaRegistrada = await ObtenerIdFincaDesdeRespuestaAsync(response);
                        TempData["MensajeExitoHtml"] = ConstruirMensajeExitoRegistroFinca(idFincaRegistrada);
                        return RedirectToAction(nameof(MisFincas));
                    }
                    catch
                    {
                        // Se intenta siguiente URL y luego fallback local
                    }
                }
            }

            var idFincaLocal = await _fincaDAO.CrearFincaAsync(dto);
            await _evaluacionTecnicaDAO.CrearEvaluacionPendienteAsync(idFincaLocal);
            TempData["MensajeExitoHtml"] = ConstruirMensajeExitoRegistroFinca(idFincaLocal, true);
            return RedirectToAction(nameof(MisFincas));
        }

        private async Task<int> ObtenerIdFincaDesdeRespuestaAsync(HttpResponseMessage response)
        {
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var documento = await JsonDocument.ParseAsync(stream);
                if (documento.RootElement.TryGetProperty("IdFinca", out var idFincaElemento)
                    && idFincaElemento.TryGetInt32(out var idFinca))
                {
                    return idFinca;
                }
            }
            catch
            {
                // Si no se puede leer el cuerpo, se mantiene el fallback en 0.
            }

            return 0;
        }

        private string ConstruirMensajeExitoRegistroFinca(int idFinca, bool modoLocal = false)
        {
            var sufijoModo = modoLocal ? " (modo local)" : string.Empty;
            var mensajeBase = $"Finca registrada correctamente{sufijoModo}.";
            if (idFinca <= 0)
            {
                return mensajeBase;
            }

            var urlDetalle = Url.Action("DetalleFinca", "Fincas", new { id = idFinca }) ?? "#";
            return $"{mensajeBase} <a href=\"{urlDetalle}\">Ver detalle de la finca</a>.";
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

        private void CargarCatalogosFormularioFinca()
        {
            var pendientes = _fincaDAO.ObtenerCatalogoFactorAsync("Pendiente").GetAwaiter().GetResult();
            var vegetaciones = _fincaDAO.ObtenerCatalogoFactorAsync("Vegetacion").GetAwaiter().GetResult();
            var usosSuelo = _fincaDAO.ObtenerCatalogoFactorAsync("UsoSuelo").GetAwaiter().GetResult();

            ViewBag.CatalogoPendiente = MezclarCatalogoBaseYBd(
                new List<string> { "Plana", "Suave", "Moderada", "Inclinada", "Muy inclinada", "Escarpada" },
                pendientes
            );

            ViewBag.CatalogoVegetacion = MezclarCatalogoBaseYBd(
                new List<string>
                {
                    "Bosque primario", "Bosque secundario", "Plantación forestal", "Pasto",
                    "Matorral", "Humedal", "Manglar", "Tacotal", "Cultivo mixto", "Regeneración natural"
                },
                vegetaciones
            );

            ViewBag.CatalogoUsoSuelo = MezclarCatalogoBaseYBd(
                new List<string>
                {
                    "Conservación", "Producción forestal", "Agroforestal", "Ganadería", "Uso mixto",
                    "Protección hídrica", "Recuperación ecológica", "Silvopastoril", "Reforestación", "Corredor biológico"
                },
                usosSuelo
            );
        }

        private static List<string> MezclarCatalogoBaseYBd(List<string> baseCatalogo, List<string> catalogoBd)
        {
            var resultado = new List<string>(baseCatalogo);
            var set = new HashSet<string>(baseCatalogo, StringComparer.OrdinalIgnoreCase);
            foreach (var valorBd in catalogoBd)
            {
                if (!string.IsNullOrWhiteSpace(valorBd))
                {
                    var valorNormalizado = valorBd.Trim();
                    if (set.Add(valorNormalizado))
                    {
                        resultado.Add(valorNormalizado);
                    }
                }
            }

            return resultado;
        }
    }
}
