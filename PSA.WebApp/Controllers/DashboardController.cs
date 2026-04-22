using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs.Evaluaciones;
using PSA.WebApp.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DashboardController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var rol = User.FindFirstValue(ClaimTypes.Role);
            return rol switch
            {
                "1" => RedirectToAction(nameof(Administrador)),
                "3" => RedirectToAction(nameof(Ingeniero)),
                "2" => RedirectToAction(nameof(Dueno)),
                _ => RedirectToAction("IniciarSesion", "Autenticacion")
            };
        }

        [HttpGet]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> Dueno()
        {
            var idUsuario = ObtenerIdUsuarioSesion();
            if (idUsuario <= 0) return RedirectToAction("IniciarSesion", "Autenticacion");

            var client = _httpClientFactory.CreateClient("AuthApi");
            var resumen = await client.GetFromJsonAsync<DashboardDuenoApiModel>($"api/Dashboard/dueno-resumen/{idUsuario}") ?? new();
            return View(new DashboardDuenoViewModel
            {
                FincasRegistradas = resumen.FincasRegistradas,
                EvaluacionesPendientes = resumen.EvaluacionesPendientes,
                CuotasPorConfirmar = resumen.CuotasPorConfirmar,
                ActividadReciente = resumen.Actividad.Select(x => new ActividadDashboardViewModel { Titulo = x.Mensaje }).ToList()
            });
        }

        [HttpGet]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> Ingeniero()
        {
            var idUsuario = ObtenerIdUsuarioSesion();
            var client = _httpClientFactory.CreateClient("AuthApi");
            var resumen = await client.GetFromJsonAsync<DashboardIngenieroApiModel>($"api/Dashboard/ingeniero-resumen/{idUsuario}") ?? new();
            var pendientes = await client.GetFromJsonAsync<List<BandejaEvaluacionPendienteDTO>>("api/EvaluacionesTecnicas/bandeja-pendientes") ?? new();
            ViewBag.ForecastProvincias = GenerarPronosticoHoy(pendientes);

            return View(new DashboardIngenieroViewModel
            {
                FincasPendientes = resumen.FincasPendientes,
                EvaluacionesAbiertas = resumen.EvaluacionesAbiertas,
                DecisionesMesActual = resumen.DecisionesMesActual,
                ProximasAcciones = resumen.ProximasAcciones.Select(x => new ActividadDashboardViewModel { Titulo = x.NombreFinca }).ToList(),
                ColaPendientesVisita = pendientes
                    .Where(p => string.Equals(p.EstadoEvaluacion, "Pendiente", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(p.EstadoEvaluacion, "En proceso", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.EstadoEvaluacion == "En proceso" ? 1 : 0)
                    .ThenBy(p => p.IdEvaluacion)
                    .Take(8)
                    .ToList()
            });
        }

        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> Administrador()
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var resumen = await client.GetFromJsonAsync<DashboardAdminApiModel>("api/Dashboard/administrador-resumen") ?? new();
            return View(new DashboardAdministradorViewModel
            {
                UsuariosActivos = resumen.UsuariosActivos,
                CuentasPorValidar = resumen.CuentasPorValidar,
                EventosAuditoria24h = resumen.EventosAuditoria24h,
                Alertas = resumen.Alertas.Select(x => new ActividadDashboardViewModel { Titulo = x }).ToList()
            });
        }

        private int ObtenerIdUsuarioSesion() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private static Dictionary<string, string> GenerarPronosticoHoy(IEnumerable<BandejaEvaluacionPendienteDTO> pendientes)
        {
            var pronosticoBase = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["San José"] = "Parcialmente nublado",
                ["Alajuela"] = "Lluvias aisladas",
                ["Cartago"] = "Lluvioso",
                ["Heredia"] = "Parcialmente nublado",
                ["Guanacaste"] = "Soleado",
                ["Puntarenas"] = "Lluvias aisladas",
                ["Limón"] = "Lluvioso"
            };

            var provinciasPendientes = pendientes
                .Select(p => p.Provincia)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (provinciasPendientes.Count == 0) return pronosticoBase;

            var orden = provinciasPendientes
                .Concat(pronosticoBase.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var provincia in orden)
            {
                resultado[provincia] = pronosticoBase.TryGetValue(provincia, out var valor)
                    ? valor
                    : "Condiciones variables";
            }

            return resultado;
        }

        public class DashboardDuenoApiModel { public int FincasRegistradas { get; set; } public int EvaluacionesPendientes { get; set; } public int CuotasPorConfirmar { get; set; } public List<ActividadApiModel> Actividad { get; set; } = new(); }
        public class DashboardIngenieroApiModel { public int FincasPendientes { get; set; } public int EvaluacionesAbiertas { get; set; } public int DecisionesMesActual { get; set; } public List<AccionApiModel> ProximasAcciones { get; set; } = new(); }
        public class DashboardAdminApiModel { public int UsuariosActivos { get; set; } public int CuentasPorValidar { get; set; } public int EventosAuditoria24h { get; set; } public List<string> Alertas { get; set; } = new(); }
        public class ActividadApiModel { public string Mensaje { get; set; } = string.Empty; }
        public class AccionApiModel { public int IdFinca { get; set; } public string NombreFinca { get; set; } = string.Empty; }
    }
}
