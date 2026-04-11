using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.WebApp.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "1,2,3")]
    public class ReportesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ReportesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? anio = null, int? mes = null)
        {
            ViewBag.ModuloActivo = "reportes";
            ViewBag.RolActivo = ObtenerNombreRol();
            ViewBag.TituloPagina = "Reportes";
            ViewBag.SubtituloPagina = "Consulte reportes y análisis según su rol dentro del sistema.";
            ViewBag.BreadcrumbActual = "Reportes";

            var modelo = new ReportesIndexViewModel
            {
                Anio = anio,
                Mes = mes
            };

            var rolId = User.FindFirstValue(ClaimTypes.Role);
            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idClaim) ? idClaim : 0;
            var client = _httpClientFactory.CreateClient("AuthApi");

            if (rolId == "2" && idUsuario > 0)
            {
                var query = $"?anio={anio}&mes={mes}";
                modelo.FincasDueno = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaDuenoReporteDTO>>($"api/Reportes/dueno/{idUsuario}/fincas") ?? new();
                modelo.PagosDueno = await client.GetFromJsonAsync<PSA.EntidadesDTO.DTOs.Reportes.ReportePagosDuenoDTO>($"api/Reportes/dueno/{idUsuario}/pagos{query}") ?? new();
                modelo.TransaccionesDueno = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemTransaccionDuenoDTO>>($"api/Reportes/dueno/{idUsuario}/transacciones{query}") ?? new();
            }
            else if (rolId == "3" && idUsuario > 0)
            {
                var query = $"?anio={anio}&mes={mes}";
                modelo.FincasPendientesIngeniero = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaPendienteIngenieroDTO>>("api/Reportes/ingeniero/fincas-pendientes") ?? new();
                modelo.EvaluacionesIngeniero = await client.GetFromJsonAsync<PSA.EntidadesDTO.DTOs.Reportes.ReporteEvaluacionesIngenieroDTO>($"api/Reportes/ingeniero/{idUsuario}/evaluaciones{query}") ?? new();
                modelo.ReporteTecnicoFinca = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemTecnicoFincaDTO>>($"api/Reportes/ingeniero/{idUsuario}/tecnico-finca{query}") ?? new();
            }
            else if (rolId == "1")
            {
                var query = $"?anio={anio}&mes={mes}";
                modelo.PagosPorUbicacion = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemPagoUbicacionDTO>>($"api/Reportes/administrador/pagos-ubicacion{query}") ?? new();
                modelo.ResumenActividad = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemResumenActividadDTO>>("api/Reportes/administrador/resumen-actividad") ?? new();
                modelo.UsuariosRoles = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemUsuarioRolReporteDTO>>("api/Reportes/administrador/usuarios-roles") ?? new();
                modelo.FincasPorEstado = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaEstadoAdminDTO>>("api/Reportes/administrador/fincas-estado") ?? new();
                modelo.EvaluacionesAdmin = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemEvaluacionAdminDTO>>($"api/Reportes/administrador/evaluaciones-tecnicas{query}") ?? new();
                modelo.PagosAdmin = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemPagosAdminDTO>>($"api/Reportes/administrador/pagos?anio={anio}") ?? new();
                modelo.AuditoriaCritica = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemAuditoriaCriticaDTO>>("api/Reportes/administrador/auditoria-critica?top=50") ?? new();
            }

            return View(modelo);
        }

        private string ObtenerNombreRol()
        {
            var rolId = User.FindFirstValue(ClaimTypes.Role);
            return rolId switch
            {
                "1" => "Administrador",
                "3" => "Ingeniero",
                _ => "Dueno"
            };
        }
    }
}
