using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs.Evaluaciones;
using PSA.WebApp.Models;
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
            var client = _httpClientFactory.CreateClient("AuthApi");
            var detalle = await client.GetFromJsonAsync<DetalleFincaParaEvaluacionDTO>($"api/EvaluacionesTecnicas/{idEvaluacion}/detalle");
            if (detalle == null) return RedirectToAction(nameof(FincasPendientes));

            return View(new NuevaEvaluacionViewModel
            {
                Detalle = detalle,
                Formulario = new RegistrarResultadoEvaluacionDTO { FechaVisita = DateTime.Today }
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
                return View(model);
            }

            TempData["MensajeExito"] = "Evaluación técnica guardada correctamente.";
            return RedirectToAction(nameof(HistorialEvaluaciones));
        }

        [HttpGet]
        public async Task<IActionResult> HistorialEvaluaciones(int? anio = null, int? mes = null, string? estadoEvaluacion = null, string? decisionTecnica = null)
        {
            var idIngeniero = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var client = _httpClientFactory.CreateClient("AuthApi");
            var url = $"api/EvaluacionesTecnicas/reportes?anio={anio}&mes={mes}&estadoEvaluacion={estadoEvaluacion}&decisionTecnica={decisionTecnica}&idIngeniero={idIngeniero}";
            var reporte = await client.GetFromJsonAsync<ReporteEvaluacionesDTO>(url) ?? new ReporteEvaluacionesDTO();
            return View(new ReporteEvaluacionesViewModel { Anio = anio, Mes = mes, EstadoEvaluacion = estadoEvaluacion, DecisionTecnica = decisionTecnica, Reporte = reporte });
        }

        [HttpGet]
        public Task<IActionResult> EvaluacionesEnProceso()
            => Task.FromResult<IActionResult>(RedirectToAction(nameof(FincasPendientes)));

        [HttpGet]
        public async Task<IActionResult> FincasIngeniero()
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var reporte = await client.GetFromJsonAsync<ReporteEvaluacionesDTO>("api/EvaluacionesTecnicas/reportes") ?? new ReporteEvaluacionesDTO();
            return View(reporte.Evaluaciones);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleEvaluacion(int idEvaluacion)
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var detalle = await client.GetFromJsonAsync<DetalleFincaParaEvaluacionDTO>($"api/EvaluacionesTecnicas/{idEvaluacion}/detalle") ?? new DetalleFincaParaEvaluacionDTO();
            return View(detalle);
        }
    }
}
