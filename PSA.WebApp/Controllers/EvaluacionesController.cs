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
        private readonly FincaDAO _fincaDAO;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public EvaluacionesController(
            EvaluacionTecnicaDAO evaluacionTecnicaDAO,
            FincaDAO fincaDAO,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _evaluacionTecnicaDAO = evaluacionTecnicaDAO;
            _fincaDAO = fincaDAO;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public async Task<IActionResult> FincasPendientes(string estado = "Todos")
        {
            CargarContextoBase("Fincas pendientes", "Revise y tome evaluaciones técnicas pendientes o en proceso.");
            var pendientes = await ObtenerBandejaDesdeApiConFallbackAsync();

            if (!string.Equals(estado, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                pendientes = pendientes
                    .Where(x => string.Equals(x.EstadoEvaluacion, estado, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var model = new BandejaEvaluacionesViewModel
            {
                Pendientes = pendientes,
                EstadoFiltro = estado
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
            CargarCatalogosEvaluacionTecnica();

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
            CargarCatalogosEvaluacionTecnica();

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

        [HttpGet]
        public async Task<IActionResult> FincasIngeniero()
        {
            CargarContextoBase("Fincas del ingeniero", "Vista consolidada de evaluaciones asignadas y completadas.");
            var reporte = await ObtenerReporteDesdeApiConFallbackAsync(null, null, null, null);
            return View(reporte.Evaluaciones);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleEvaluacion(int idEvaluacion)
        {
            if (idEvaluacion <= 0)
            {
                return RedirectToAction(nameof(FincasPendientes));
            }

            CargarContextoBase("Detalle de evaluación", "Detalle completo técnico de la evaluación seleccionada.");
            var detalle = await ObtenerDetalleDesdeApiConFallbackAsync(idEvaluacion);
            if (detalle == null)
            {
                TempData["MensajeError"] = "No se encontró la evaluación solicitada.";
                return RedirectToAction(nameof(FincasPendientes));
            }

            return View(detalle);
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

        private void CargarCatalogosEvaluacionTecnica()
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

            ViewBag.CatalogoEstadoEvaluacion = new List<string>
            {
                "Pendiente",
                "En proceso",
                "Evaluada – No califica",
                "Evaluada – Califica",
                "Pendiente de cuenta bancaria",
                "Pendiente de aprobación final de pago",
                "Pagos activos",
                "Finalizada"
            };
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
