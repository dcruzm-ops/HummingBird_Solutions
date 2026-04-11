using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.WebApp.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PSA.WebApp.Controllers;

[Authorize(Roles = "1,2,3")]
public class ReportesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ReportesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ConfigurarVistaBase("Inicio de reportes", "Seleccione un reporte disponible para su rol.");
        var rolId = User.FindFirstValue(ClaimTypes.Role);
        return View(new ReportesIndexViewModel
        {
            EsAdmin = rolId == "1",
            EsDueno = rolId == "2",
            EsIngeniero = rolId == "3"
        });
    }

    [HttpGet, Authorize(Roles = "2")]
    public async Task<IActionResult> DuenoFincas(string? estadoFinca = null, string? estadoEvaluacion = null)
    {
        ConfigurarVistaBase("Reporte de mis fincas", "Fincas y estado de evaluación.");
        var idUsuario = ObtenerIdUsuario();
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaDuenoReporteDTO>>($"api/Reportes/dueno/{idUsuario}/fincas") ?? new();

        if (!string.IsNullOrWhiteSpace(estadoFinca)) items = items.Where(x => x.EstadoFinca.Equals(estadoFinca, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(estadoEvaluacion)) items = items.Where(x => x.EstadoEvaluacion.Equals(estadoEvaluacion, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteDuenoFincasViewModel { EstadoFinca = estadoFinca, EstadoEvaluacion = estadoEvaluacion, Items = items });
    }

    [HttpGet, Authorize(Roles = "2")]
    public async Task<IActionResult> DuenoPagos(int? anio = null, int? mes = null, string? estadoCuota = null)
    {
        ConfigurarVistaBase("Reporte de pagos", "Plan de pagos, cuotas y transacciones.");
        var idUsuario = ObtenerIdUsuario();
        var client = _httpClientFactory.CreateClient("AuthApi");
        var query = $"?anio={anio}&mes={mes}";
        var pagos = await client.GetFromJsonAsync<PSA.EntidadesDTO.DTOs.Reportes.ReportePagosDuenoDTO>($"api/Reportes/dueno/{idUsuario}/pagos{query}") ?? new();
        var transacciones = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemTransaccionDuenoDTO>>($"api/Reportes/dueno/{idUsuario}/transacciones{query}") ?? new();

        if (!string.IsNullOrWhiteSpace(estadoCuota))
        {
            pagos.PagosMensuales = pagos.PagosMensuales.Where(x => x.EstadoCuota.Equals(estadoCuota, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return View(new ReporteDuenoPagosViewModel { Anio = anio, Mes = mes, EstadoCuota = estadoCuota, ReportePagos = pagos, Transacciones = transacciones });
    }

    [HttpGet, Authorize(Roles = "3")]
    public async Task<IActionResult> IngenieroPendientes(string? provincia = null)
    {
        ConfigurarVistaBase("Fincas pendientes por evaluar", "Bandeja de pendientes para ingeniería.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaPendienteIngenieroDTO>>("api/Reportes/ingeniero/fincas-pendientes") ?? new();

        if (!string.IsNullOrWhiteSpace(provincia)) items = items.Where(x => x.Provincia.Equals(provincia, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteIngenieroPendientesViewModel { Provincia = provincia, Items = items });
    }

    [HttpGet, Authorize(Roles = "3")]
    public async Task<IActionResult> IngenieroEvaluaciones(int? anio = null, int? mes = null, string? estadoEvaluacion = null)
    {
        ConfigurarVistaBase("Evaluaciones en proceso/finalizadas", "Reporte de evaluaciones del ingeniero.");
        var idUsuario = ObtenerIdUsuario();
        var client = _httpClientFactory.CreateClient("AuthApi");
        var query = $"?anio={anio}&mes={mes}";
        var reporte = await client.GetFromJsonAsync<PSA.EntidadesDTO.DTOs.Reportes.ReporteEvaluacionesIngenieroDTO>($"api/Reportes/ingeniero/{idUsuario}/evaluaciones{query}") ?? new();

        if (!string.IsNullOrWhiteSpace(estadoEvaluacion))
        {
            reporte.Evaluaciones = reporte.Evaluaciones.Where(x => x.EstadoEvaluacion.Equals(estadoEvaluacion, StringComparison.OrdinalIgnoreCase)).ToList();
            reporte.Total = reporte.Evaluaciones.Count;
        }

        return View(new ReporteIngenieroEvaluacionesViewModel { Anio = anio, Mes = mes, EstadoEvaluacion = estadoEvaluacion, Reporte = reporte });
    }

    [HttpGet, Authorize(Roles = "3")]
    public async Task<IActionResult> IngenieroTecnico(int? anio = null, int? mes = null, string? decision = null)
    {
        ConfigurarVistaBase("Reporte técnico por finca", "Ajustes, decisión, visita y observaciones.");
        var idUsuario = ObtenerIdUsuario();
        var client = _httpClientFactory.CreateClient("AuthApi");
        var query = $"?anio={anio}&mes={mes}";
        var items = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemTecnicoFincaDTO>>($"api/Reportes/ingeniero/{idUsuario}/tecnico-finca{query}") ?? new();

        if (!string.IsNullOrWhiteSpace(decision)) items = items.Where(x => string.Equals(x.DecisionTecnica, decision, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteIngenieroTecnicoViewModel { Anio = anio, Mes = mes, Decision = decision, Items = items });
    }

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminUsuarios(string? rol = null, string? estado = null)
    {
        ConfigurarVistaBase("Usuarios y roles", "Estados de usuarios y asignación de roles.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemUsuarioRolReporteDTO>>("api/Reportes/administrador/usuarios-roles") ?? new();

        if (!string.IsNullOrWhiteSpace(rol)) items = items.Where(x => x.Rol.Equals(rol, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(estado)) items = items.Where(x => x.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteAdminUsuariosViewModel { Rol = rol, Estado = estado, Items = items });
    }

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminEvaluaciones(int? anio = null, int? mes = null, string? estado = null, string? decision = null)
    {
        ConfigurarVistaBase("Evaluaciones técnicas", "Quién evaluó, cuándo, resultado y observaciones.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var query = $"?anio={anio}&mes={mes}";
        var items = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemEvaluacionAdminDTO>>($"api/Reportes/administrador/evaluaciones-tecnicas{query}") ?? new();

        if (!string.IsNullOrWhiteSpace(estado)) items = items.Where(x => x.EstadoEvaluacion.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(decision)) items = items.Where(x => string.Equals(x.DecisionTecnica, decision, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteAdminEvaluacionesViewModel { Anio = anio, Mes = mes, Estado = estado, Decision = decision, Items = items });
    }

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminPagos(int? anio = null, string? estadoCuotas = null)
    {
        ConfigurarVistaBase("Reporte de pagos", "Planes generados y estado de cuotas.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var planes = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemPagosAdminDTO>>($"api/Reportes/administrador/pagos?anio={anio}") ?? new();
        var porUbicacion = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemPagoUbicacionDTO>>($"api/Reportes/administrador/pagos-ubicacion?anio={anio}") ?? new();

        if (string.Equals(estadoCuotas, "Pendientes", StringComparison.OrdinalIgnoreCase))
            planes = planes.Where(x => x.CuotasPendientes > 0).ToList();
        else if (string.Equals(estadoCuotas, "Pagadas", StringComparison.OrdinalIgnoreCase))
            planes = planes.Where(x => x.CuotasPendientes == 0).ToList();

        return View(new ReporteAdminPagosViewModel { Anio = anio, EstadoCuotas = estadoCuotas, Planes = planes, PagosPorUbicacion = porUbicacion });
    }

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminAuditoria(string? modulo = null, string? accion = null, int top = 50)
    {
        ConfigurarVistaBase("Auditoría crítica", "Movimientos críticos del sistema.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemAuditoriaCriticaDTO>>($"api/Reportes/administrador/auditoria-critica?top={top}") ?? new();

        if (!string.IsNullOrWhiteSpace(modulo)) items = items.Where(x => x.Modulo.Equals(modulo, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(accion)) items = items.Where(x => x.Accion.Equals(accion, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteAdminAuditoriaViewModel { Modulo = modulo, Accion = accion, Top = top, Items = items });
    }

    private int ObtenerIdUsuario() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private void ConfigurarVistaBase(string titulo, string subtitulo)
    {
        ViewBag.ModuloActivo = "reportes";
        ViewBag.RolActivo = ObtenerNombreRol();
        ViewBag.TituloPagina = titulo;
        ViewBag.SubtituloPagina = subtitulo;
        ViewBag.BreadcrumbActual = "Reportes";
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
