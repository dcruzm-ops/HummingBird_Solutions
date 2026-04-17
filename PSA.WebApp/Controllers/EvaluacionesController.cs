using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs.Evaluaciones;
using PSA.EntidadesDTO.DTOs.Fincas;
using PSA.WebApp.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "3")]
    public class EvaluacionesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EvaluacionesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> FincasPendientes(string estado = "Todos")
        {
            ConfigurarVistaBase(
                "Gestión de visitas técnicas",
                "Cola de propiedades pendientes de visita y seguimiento de evaluaciones.");
            var client = _httpClientFactory.CreateClient("AuthApi");
            var pendientes = await client.GetFromJsonAsync<List<BandejaEvaluacionPendienteDTO>>("api/EvaluacionesTecnicas/bandeja-pendientes") ?? new();
            if (!string.Equals(estado, "Todos", StringComparison.OrdinalIgnoreCase))
                pendientes = pendientes.Where(x => string.Equals(x.EstadoEvaluacion, estado, StringComparison.OrdinalIgnoreCase)).ToList();

            return View(new BandejaEvaluacionesViewModel { Pendientes = pendientes, EstadoFiltro = estado });
        }

        [HttpGet]
        public async Task<IActionResult> NuevaEvaluacion(int idEvaluacion)
        {
            if (idEvaluacion <= 0) return RedirectToAction(nameof(FincasPendientes));
            ConfigurarVistaBase(
                "Gestión de visitas técnicas",
                "Registro de resultados de la evaluación, ajustes y carga de evidencia.");
            var client = _httpClientFactory.CreateClient("AuthApi");
            var detalle = await client.GetFromJsonAsync<DetalleFincaParaEvaluacionDTO>($"api/EvaluacionesTecnicas/{idEvaluacion}/detalle");
            if (detalle == null) return RedirectToAction(nameof(FincasPendientes));
            var evidencias = await ObtenerEvidenciasPorFincaAsync(client, detalle.IdFinca);

            CargarCatalogosEvaluacion();

            return View(new NuevaEvaluacionViewModel
            {
                Detalle = detalle,
                Formulario = new RegistrarResultadoEvaluacionDTO { FechaVisita = DateTime.Today },
                EvidenciasExistentes = evidencias
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaEvaluacion(int idEvaluacion, NuevaEvaluacionViewModel model)
        {
            if (idEvaluacion <= 0 || model?.Formulario == null) return RedirectToAction(nameof(FincasPendientes));
            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PutAsJsonAsync($"api/EvaluacionesTecnicas/{idEvaluacion}/resultado", model.Formulario);
            if (!response.IsSuccessStatusCode)
            {
                TempData["MensajeError"] = "No fue posible guardar la evaluación.";
                model.Detalle = await client.GetFromJsonAsync<DetalleFincaParaEvaluacionDTO>($"api/EvaluacionesTecnicas/{idEvaluacion}/detalle") ?? new();
                model.EvidenciasExistentes = await ObtenerEvidenciasPorFincaAsync(client, model.Detalle.IdFinca);
                CargarCatalogosEvaluacion();
                return View(model);
            }

            var evidenciasAdjuntas = model.Evidencias?.Where(e => e != null && e.Length > 0).ToList() ?? new List<IFormFile>();
            var totalEvidencias = evidenciasAdjuntas.Count;
            if (totalEvidencias > 0)
            {
                var detalle = await client.GetFromJsonAsync<DetalleFincaParaEvaluacionDTO>($"api/EvaluacionesTecnicas/{idEvaluacion}/detalle");
                var idFinca = detalle?.IdFinca ?? 0;
                var idUsuario = ObtenerIdUsuarioSesion();

                if (idFinca > 0 && idUsuario > 0)
                {
                    var evidenciaSubida = await SubirEvidenciasAsync(client, idFinca, idUsuario, evidenciasAdjuntas);
                    if (!evidenciaSubida)
                    {
                        TempData["MensajeError"] = "La evaluación se guardó, pero no fue posible cargar la evidencia adjunta.";
                    }
                }
                else
                {
                    TempData["MensajeError"] = "La evaluación se guardó, pero no fue posible identificar la finca para cargar evidencia.";
                }
            }

            TempData["MensajeExito"] = totalEvidencias > 0
                ? "Evaluación técnica guardada correctamente junto con la evidencia adjunta."
                : "Evaluación técnica guardada correctamente.";
            return RedirectToAction(nameof(HistorialEvaluaciones));
        }

        [HttpGet]
        public async Task<IActionResult> HistorialEvaluaciones(int? anio = null, int? mes = null, string? estadoEvaluacion = null, string? decisionTecnica = null)
        {
            ConfigurarVistaBase(
                "Gestión de visitas técnicas",
                "Historial y métricas de resultados de evaluaciones técnicas.");
            var idIngeniero = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var client = _httpClientFactory.CreateClient("AuthApi");
            var url = $"api/EvaluacionesTecnicas/reportes?anio={anio}&mes={mes}&estadoEvaluacion={estadoEvaluacion}&decisionTecnica={decisionTecnica}&idIngeniero={idIngeniero}";
            var reporte = await ObtenerSeguroDesdeApiAsync<ReporteEvaluacionesDTO>(client, url, "No fue posible aplicar los filtros del reporte.") ?? new ReporteEvaluacionesDTO();
            return View(new ReporteEvaluacionesViewModel { Anio = anio, Mes = mes, EstadoEvaluacion = estadoEvaluacion, DecisionTecnica = decisionTecnica, Reporte = reporte });
        }

        [HttpGet]
        public Task<IActionResult> EvaluacionesEnProceso()
            => Task.FromResult<IActionResult>(RedirectToAction(nameof(FincasPendientes)));

        [HttpGet]
        public async Task<IActionResult> FincasIngeniero()
        {
            ConfigurarVistaBase(
                "Gestión de visitas técnicas",
                "Resumen de fincas y evaluaciones asignadas al ingeniero.");
            var client = _httpClientFactory.CreateClient("AuthApi");
            var reporte = await client.GetFromJsonAsync<ReporteEvaluacionesDTO>("api/EvaluacionesTecnicas/reportes") ?? new ReporteEvaluacionesDTO();
            return View(reporte.Evaluaciones);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleEvaluacion(int idEvaluacion)
        {
            ConfigurarVistaBase(
                "Gestión de visitas técnicas",
                "Detalle de visita técnica y trazabilidad de ajustes.");
            var client = _httpClientFactory.CreateClient("AuthApi");
            var detalle = await client.GetFromJsonAsync<DetalleFincaParaEvaluacionDTO>($"api/EvaluacionesTecnicas/{idEvaluacion}/detalle") ?? new DetalleFincaParaEvaluacionDTO();
            return View(detalle);
        }

        private int ObtenerIdUsuarioSesion() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private static async Task<List<FincaEvidenciaDTO>> ObtenerEvidenciasPorFincaAsync(HttpClient client, int idFinca)
        {
            if (idFinca <= 0) return new();
            try
            {
                using var response = await client.GetAsync($"api/FincaEvidencias/finca/{idFinca}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return new();

                if (!response.IsSuccessStatusCode)
                    return new();

                return await response.Content.ReadFromJsonAsync<List<FincaEvidenciaDTO>>() ?? new();
            }
            catch (HttpRequestException)
            {
                return new();
            }
        }

        private async Task<T?> ObtenerSeguroDesdeApiAsync<T>(HttpClient client, string url, string mensajeError)
        {
            try
            {
                return await client.GetFromJsonAsync<T>(url);
            }
            catch (HttpRequestException)
            {
                TempData["MensajeError"] = mensajeError;
                return default;
            }
            catch (NotSupportedException)
            {
                TempData["MensajeError"] = mensajeError;
                return default;
            }
        }

        private static async Task<bool> SubirEvidenciasAsync(HttpClient client, int idFinca, int idUsuario, List<IFormFile> archivos)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(idFinca.ToString()), "idFinca");
            form.Add(new StringContent(idUsuario.ToString()), "cargadoPor");

            foreach (var archivo in archivos.Where(a => a != null && a.Length > 0))
            {
                var contenido = new StreamContent(archivo.OpenReadStream());
                contenido.Headers.ContentType = new MediaTypeHeaderValue(archivo.ContentType ?? "application/octet-stream");
                form.Add(contenido, "archivos", archivo.FileName);
            }

            var response = await client.PostAsync("api/FincaEvidencias/subir", form);
            return response.IsSuccessStatusCode;
        }

        private void ConfigurarVistaBase(string titulo, string subtitulo)
        {
            ViewBag.ModuloActivo = "evaluaciones";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.TituloPagina = titulo;
            ViewBag.SubtituloPagina = subtitulo;
            ViewBag.BreadcrumbPadreTexto = "Inicio";
            ViewBag.BreadcrumbPadreUrl = Url.Action("Ingeniero", "Dashboard");
            ViewBag.BreadcrumbActual = titulo;
        }

        private void CargarCatalogosEvaluacion()
        {
            ViewBag.CatalogoPendiente = new[] { "Plana", "Inclinada", "Muy inclinada" };
            ViewBag.CatalogoVegetacion = new[] { "Bosque primario", "Bosque secundario", "Plantación forestal", "Pasto" };
            ViewBag.CatalogoUsoSuelo = new[] { "Conservación", "Producción forestal", "Agroforestal", "Ganadería", "Mixto" };
            ViewBag.CatalogoEstadoEvaluacion = new[] { "Pendiente", "En proceso", "Evaluada – Califica", "Evaluada – No califica" };
        }
    }
}
