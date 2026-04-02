using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Evaluaciones;
using PSA.WebApp.Models;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Linq;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "3")]
    public class EvaluacionesController : Controller
    {
        private readonly EvaluacionTecnicaDAO _evaluacionTecnicaDAO;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public EvaluacionesController(
            EvaluacionTecnicaDAO evaluacionTecnicaDAO,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _evaluacionTecnicaDAO = evaluacionTecnicaDAO;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public async Task<IActionResult> FincasPendientes()
        {
            CargarContextoBase("Fincas pendientes", "Revise y tome evaluaciones técnicas pendientes o en proceso.");
            var model = new BandejaEvaluacionesViewModel
            {
                Pendientes = await ObtenerBandejaDesdeApiConFallbackAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> NuevaEvaluacion(int idEvaluacion)
        {
            if (idEvaluacion <= 0)
            {
                return RedirectToAction(nameof(FincasPendientes));
            }

            CargarContextoBase("Nueva evaluación técnica", "Registre visita, observaciones, decisión y ajustes técnicos.");
            ViewBag.BreadcrumbPadreTexto = "Fincas pendientes";
            ViewBag.BreadcrumbPadreUrl = Url.Action(nameof(FincasPendientes));
            ViewBag.BreadcrumbActual = "Nueva evaluación";

            var detalle = await ObtenerDetalleDesdeApiConFallbackAsync(idEvaluacion);
            if (detalle == null)
            {
                TempData["MensajeError"] = "No se encontró el detalle de la evaluación seleccionada.";
                return RedirectToAction(nameof(FincasPendientes));
            }

            var idIngeniero = ObtenerIdUsuarioSesion();
            if (idIngeniero > 0
                && string.Equals(detalle.EstadoEvaluacion, EstadosEvaluacionTecnica.Pendiente, StringComparison.OrdinalIgnoreCase)
                && (!detalle.IdIngeniero.HasValue || detalle.IdIngeniero.Value == idIngeniero))
            {
                await AsignarEvaluacionDesdeApiConFallbackAsync(idEvaluacion, idIngeniero);
                detalle = await ObtenerDetalleDesdeApiConFallbackAsync(idEvaluacion) ?? detalle;
            }

            if (detalle.IdIngeniero.HasValue && detalle.IdIngeniero.Value != idIngeniero)
            {
                TempData["MensajeError"] = "Esta evaluación ya fue tomada por otro ingeniero.";
                return RedirectToAction(nameof(FincasPendientes));
            }

            return View(new NuevaEvaluacionViewModel
            {
                Detalle = detalle,
                Formulario = new RegistrarResultadoEvaluacionDTO
                {
                    FechaVisita = DateTime.Today,
                    HectareasAjustadas = detalle.Hectareas,
                    VegetacionAjustada = detalle.Vegetacion,
                    RecursosHidricosAjustado = detalle.TieneRecursosHidricos,
                    UsoSueloAjustado = detalle.UsoSuelo,
                    PendienteAjustada = detalle.Pendiente
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaEvaluacion(int idEvaluacion, NuevaEvaluacionViewModel model)
        {
            CargarContextoBase("Nueva evaluación técnica", "Registre visita, observaciones, decisión y ajustes técnicos.");
            ViewBag.BreadcrumbPadreTexto = "Fincas pendientes";
            ViewBag.BreadcrumbPadreUrl = Url.Action(nameof(FincasPendientes));
            ViewBag.BreadcrumbActual = "Nueva evaluación";

            if (idEvaluacion <= 0)
            {
                TempData["MensajeError"] = "La evaluación seleccionada no es válida.";
                return RedirectToAction(nameof(FincasPendientes));
            }

            if (model?.Formulario == null)
            {
                ModelState.AddModelError(string.Empty, "Debe completar el formulario de evaluación.");
            }

            if (model?.Formulario != null && string.IsNullOrWhiteSpace(model.Formulario.DecisionTecnica))
            {
                ModelState.AddModelError("Formulario.DecisionTecnica", "Debe seleccionar una decisión técnica.");
            }

            if (!ModelState.IsValid)
            {
                model ??= new NuevaEvaluacionViewModel();
                model.Detalle = await ObtenerDetalleDesdeApiConFallbackAsync(idEvaluacion) ?? new DetalleFincaParaEvaluacionDTO();
                return View(model);
            }

            var actualizado = await RegistrarResultadoDesdeApiConFallbackAsync(idEvaluacion, model!.Formulario);
            if (!actualizado)
            {
                TempData["MensajeError"] = "No fue posible guardar la evaluación técnica. Intente nuevamente.";
                model.Detalle = await ObtenerDetalleDesdeApiConFallbackAsync(idEvaluacion) ?? new DetalleFincaParaEvaluacionDTO();
                return View(model);
            }

            TempData["MensajeExito"] = "Evaluación técnica guardada correctamente.";
            return RedirectToAction(nameof(HistorialEvaluaciones));
        }

        [HttpGet]
        public async Task<IActionResult> EvaluacionesEnProceso()
        {
            CargarContextoBase("Evaluaciones en proceso", "Consulte evaluaciones abiertas y tareas por completar.");
            var evaluaciones = await ObtenerBandejaDesdeApiConFallbackAsync();
            var enProceso = evaluaciones
                .Where(x => string.Equals(x.EstadoEvaluacion, EstadosEvaluacionTecnica.EnProceso, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return View(enProceso);
        }

        [HttpGet]
        public async Task<IActionResult> HistorialEvaluaciones(int? anio = null, int? mes = null, string? estadoEvaluacion = null, string? decisionTecnica = null)
        {
            CargarContextoBase("Historial de evaluaciones", "Consulte reportes mensuales y anuales de evaluaciones técnicas.");
            var model = new ReporteEvaluacionesViewModel
            {
                Anio = anio,
                Mes = mes,
                EstadoEvaluacion = estadoEvaluacion,
                DecisionTecnica = decisionTecnica,
                Reporte = await ObtenerReporteDesdeApiConFallbackAsync(anio, mes, estadoEvaluacion, decisionTecnica)
            };

            return View(model);
        }

        private async Task<List<BandejaEvaluacionPendienteDTO>> ObtenerBandejaDesdeApiConFallbackAsync()
        {
            var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi");
            if (client != null)
            {
                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
                        var response = await client.GetFromJsonAsync<List<BandejaEvaluacionPendienteDTO>>(
                            $"{baseUrl}/api/EvaluacionesTecnicas/bandeja-pendientes");

                        if (response != null)
                        {
                            return response;
                        }
                    }
                    catch
                    {
                        // Probar siguiente URL
                    }
                }
            }

            return await _evaluacionTecnicaDAO.ObtenerBandejaPendientesAsync();
        }

        private async Task<DetalleFincaParaEvaluacionDTO?> ObtenerDetalleDesdeApiConFallbackAsync(int idEvaluacion)
        {
            var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi");
            if (client != null)
            {
                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
                        var response = await client.GetFromJsonAsync<DetalleFincaParaEvaluacionDTO>(
                            $"{baseUrl}/api/EvaluacionesTecnicas/{idEvaluacion}/detalle");

                        if (response != null)
                        {
                            return response;
                        }
                    }
                    catch
                    {
                        // Probar siguiente URL
                    }
                }
            }

            return await _evaluacionTecnicaDAO.ObtenerDetalleParaEvaluacionAsync(idEvaluacion);
        }

        private async Task<bool> AsignarEvaluacionDesdeApiConFallbackAsync(int idEvaluacion, int idIngeniero)
        {
            var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi");
            if (client != null)
            {
                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
                        var response = await client.PutAsJsonAsync(
                            $"{baseUrl}/api/EvaluacionesTecnicas/{idEvaluacion}/asignar",
                            new AsignarEvaluacionDTO { IdIngeniero = idIngeniero });

                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Probar siguiente URL
                    }
                }
            }

            return await _evaluacionTecnicaDAO.AsignarIngenieroAsync(idEvaluacion, idIngeniero);
        }

        private async Task<bool> RegistrarResultadoDesdeApiConFallbackAsync(int idEvaluacion, RegistrarResultadoEvaluacionDTO dto)
        {
            var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi");
            if (client != null)
            {
                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
                        var response = await client.PutAsJsonAsync(
                            $"{baseUrl}/api/EvaluacionesTecnicas/{idEvaluacion}/resultado", dto);

                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Probar siguiente URL
                    }
                }
            }

            return await _evaluacionTecnicaDAO.RegistrarResultadoAsync(idEvaluacion, dto);
        }

        private async Task<ReporteEvaluacionesDTO> ObtenerReporteDesdeApiConFallbackAsync(int? anio, int? mes, string? estadoEvaluacion, string? decisionTecnica)
        {
            var idIngeniero = ObtenerIdUsuarioSesion();
            var query = $"anio={anio}&mes={mes}&estadoEvaluacion={Uri.EscapeDataString(estadoEvaluacion ?? string.Empty)}&decisionTecnica={Uri.EscapeDataString(decisionTecnica ?? string.Empty)}&idIngeniero={idIngeniero}";
            var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi");

            if (client != null)
            {
                foreach (var baseUrl in GetApiBaseUrls())
                {
                    try
                    {
                        var response = await client.GetFromJsonAsync<ReporteEvaluacionesDTO>(
                            $"{baseUrl}/api/EvaluacionesTecnicas/reportes?{query}");

                        if (response != null)
                        {
                            return response;
                        }
                    }
                    catch
                    {
                        // Probar siguiente URL
                    }
                }
            }

            return await _evaluacionTecnicaDAO.ObtenerReporteEvaluacionesAsync(new FiltroReporteEvaluacionesDTO
            {
                Anio = anio,
                Mes = mes,
                EstadoEvaluacion = estadoEvaluacion,
                DecisionTecnica = decisionTecnica,
                IdIngeniero = idIngeniero
            });
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

        private void CargarContextoBase(string titulo, string subtitulo)
        {
            ViewBag.ModuloActivo = "evaluaciones";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.TituloPagina = titulo;
            ViewBag.SubtituloPagina = subtitulo;
            ViewBag.BreadcrumbActual = titulo;
        }
    }
}
