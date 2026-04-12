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
        ConfigurarVistaBase("Inicio de reportes", "Seleccione un reporte disponible para su rol.", true);
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
    public async Task<IActionResult> IngenieroEvaluaciones(int? anio = null, int? mes = null, string? estadoEvaluacion = null, string vistaPeriodo = "Mensual")
    {
        ConfigurarVistaBase("Evaluaciones en proceso/finalizadas", "Reporte de evaluaciones del ingeniero.");
        var idUsuario = ObtenerIdUsuario();
        var client = _httpClientFactory.CreateClient("AuthApi");
        if (string.Equals(vistaPeriodo, "Anual", StringComparison.OrdinalIgnoreCase))
        {
            mes = null;
        }

        var query = $"?anio={anio}&mes={mes}";
        var reporte = await client.GetFromJsonAsync<PSA.EntidadesDTO.DTOs.Reportes.ReporteEvaluacionesIngenieroDTO>($"api/Reportes/ingeniero/{idUsuario}/evaluaciones{query}") ?? new();

        if (!string.IsNullOrWhiteSpace(estadoEvaluacion))
        {
            reporte.Evaluaciones = reporte.Evaluaciones.Where(x => x.EstadoEvaluacion.Equals(estadoEvaluacion, StringComparison.OrdinalIgnoreCase)).ToList();
            reporte.Total = reporte.Evaluaciones.Count;
        }

        return View(new ReporteIngenieroEvaluacionesViewModel
        {
            Anio = anio,
            Mes = mes,
            VistaPeriodo = vistaPeriodo,
            EstadoEvaluacion = estadoEvaluacion,
            Reporte = reporte
        });
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
    public async Task<IActionResult> AdminPagos(
        int? anioPlanes = null,
        string? estadoCuotas = null,
        int? anioUbicacion = null,
        string? provincia = null,
        string? canton = null,
        string? distrito = null)
    {
        ConfigurarVistaBase("Reporte de pagos", "Planes generados y estado de cuotas.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var planes = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemPagosAdminDTO>>($"api/Reportes/administrador/pagos?anio={anioPlanes}") ?? new();
        var porUbicacion = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemPagoUbicacionDTO>>($"api/Reportes/administrador/pagos-ubicacion?anio={anioUbicacion}") ?? new();

        if (string.Equals(estadoCuotas, "Pendientes", StringComparison.OrdinalIgnoreCase))
            planes = planes.Where(x => x.CuotasPendientes > 0).ToList();
        else if (string.Equals(estadoCuotas, "Pagadas", StringComparison.OrdinalIgnoreCase))
            planes = planes.Where(x => x.CuotasPendientes == 0).ToList();

        if (!string.IsNullOrWhiteSpace(provincia))
            porUbicacion = porUbicacion.Where(x => x.Provincia.Equals(provincia, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(canton))
            porUbicacion = porUbicacion.Where(x => x.Canton.Equals(canton, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(distrito))
            porUbicacion = porUbicacion.Where(x => x.Distrito.Equals(distrito, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteAdminPagosViewModel
        {
            AnioPlanes = anioPlanes,
            AnioUbicacion = anioUbicacion,
            EstadoCuotas = estadoCuotas,
            Provincia = provincia,
            Canton = canton,
            Distrito = distrito,
            Planes = planes,
            PagosPorUbicacion = porUbicacion
        });
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

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminFincasEstado()
    {
        ConfigurarVistaBase("Fincas por estado", "Resumen de fincas registradas, en proceso, aprobadas y rechazadas.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaEstadoAdminDTO>>("api/Reportes/administrador/fincas-estado") ?? new();
        return View(new ReporteAdminFincasEstadoViewModel { Items = items });
    }

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminResumenActividad()
    {
        ConfigurarVistaBase("Resumen de actividad", "Indicadores clave de actividad del sistema.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await client.GetFromJsonAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemResumenActividadDTO>>("api/Reportes/administrador/resumen-actividad") ?? new();
        return View(new ReporteAdminResumenActividadViewModel { Items = items });
    }

    private int ObtenerIdUsuario() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private void ConfigurarVistaBase(string titulo, string subtitulo, bool esInicioReportes = false)
    {
        ViewBag.ModuloActivo = "reportes";
        ViewBag.RolActivo = ObtenerNombreRol();
        ViewBag.TituloPagina = titulo;
        ViewBag.SubtituloPagina = subtitulo;
        ViewBag.BreadcrumbInicioTexto = "Inicio de reportes";
        ViewBag.BreadcrumbInicioControlador = "Reportes";
        ViewBag.BreadcrumbInicioAccion = "Index";

        if (esInicioReportes)
        {
            ViewBag.BreadcrumbActual = "Inicio";
            return;
        }

        ViewBag.BreadcrumbPadreTexto = "Reportes";
        ViewBag.BreadcrumbPadreUrl = Url.Action("Index", "Reportes");
        ViewBag.BreadcrumbActual = titulo;
    }

    private string ObtenerNombreRol()
    {
        var rolId = User.FindFirstValue(ClaimTypes.Role);
        return rolId switch
        {
            "1" => "Administrador",
            "3" => "Ingeniero",
            _ => "Dueño"
        };
    }
}
