using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.WebApp.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PSA.WebApp.Controllers;

[Authorize(Roles = "1,2,3")]
public class ReportesController : AppControllerBase
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
        var items = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaDuenoReporteDTO>>(client, $"api/Reportes/dueno/{idUsuario}/fincas", "No fue posible cargar el reporte solicitado.") ?? new();

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
        var pagos = await ObtenerSeguroDesdeApiAsync<PSA.EntidadesDTO.DTOs.Reportes.ReportePagosDuenoDTO>(client, $"api/Reportes/dueno/{idUsuario}/pagos{query}", "No fue posible cargar el reporte solicitado.") ?? new();
        var transacciones = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemTransaccionDuenoDTO>>(client, $"api/Reportes/dueno/{idUsuario}/transacciones{query}", "No fue posible cargar el reporte solicitado.") ?? new();

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
        var items = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaPendienteIngenieroDTO>>(client, "api/Reportes/ingeniero/fincas-pendientes", "No fue posible cargar el reporte solicitado.") ?? new();

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
        var reporte = await ObtenerSeguroDesdeApiAsync<PSA.EntidadesDTO.DTOs.Reportes.ReporteEvaluacionesIngenieroDTO>(client, $"api/Reportes/ingeniero/{idUsuario}/evaluaciones{query}", "No fue posible cargar el reporte solicitado.") ?? new();

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
        var items = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemTecnicoFincaDTO>>(client, $"api/Reportes/ingeniero/{idUsuario}/tecnico-finca{query}", "No fue posible cargar el reporte solicitado.") ?? new();

        if (!string.IsNullOrWhiteSpace(decision)) items = items.Where(x => string.Equals(x.DecisionTecnica, decision, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteIngenieroTecnicoViewModel { Anio = anio, Mes = mes, Decision = decision, Items = items });
    }

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminUsuarios(string? rol = null, string? estado = null)
    {
        ConfigurarVistaBase("Usuarios y roles", "Estados de usuarios y asignación de roles.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemUsuarioRolReporteDTO>>(client, "api/Reportes/administrador/usuarios-roles", "No fue posible cargar el reporte solicitado.") ?? new();

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
        var items = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemEvaluacionAdminDTO>>(client, $"api/Reportes/administrador/evaluaciones-tecnicas{query}", "No fue posible cargar el reporte solicitado.") ?? new();

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
        var planes = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemPagosAdminDTO>>(client, $"api/Reportes/administrador/pagos?anio={anioPlanes}", "No fue posible cargar el reporte solicitado.") ?? new();
        var porUbicacion = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemPagoUbicacionDTO>>(client, $"api/Reportes/administrador/pagos-ubicacion?anio={anioUbicacion}", "No fue posible cargar el reporte solicitado.") ?? new();

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
        var items = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemAuditoriaCriticaDTO>>(client, $"api/Reportes/administrador/auditoria-critica?top={top}", "No fue posible cargar el reporte solicitado.") ?? new();

        if (!string.IsNullOrWhiteSpace(modulo)) items = items.Where(x => x.Modulo.Equals(modulo, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(accion)) items = items.Where(x => x.Accion.Equals(accion, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ReporteAdminAuditoriaViewModel { Modulo = modulo, Accion = accion, Top = top, Items = items });
    }

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminFincasEstado()
    {
        ConfigurarVistaBase("Fincas por estado", "Resumen de fincas registradas, en proceso, aprobadas y rechazadas.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemFincaEstadoAdminDTO>>(client, "api/Reportes/administrador/fincas-estado", "No fue posible cargar el reporte solicitado.") ?? new();
        return View(new ReporteAdminFincasEstadoViewModel { Items = items });
    }

    [HttpGet, Authorize(Roles = "1")]
    public async Task<IActionResult> AdminResumenActividad()
    {
        ConfigurarVistaBase("Resumen de actividad", "Indicadores clave de actividad del sistema.");
        var client = _httpClientFactory.CreateClient("AuthApi");
        var items = await ObtenerSeguroDesdeApiAsync<List<PSA.EntidadesDTO.DTOs.Reportes.ItemResumenActividadDTO>>(client, "api/Reportes/administrador/resumen-actividad", "No fue posible cargar el reporte solicitado.") ?? new();
        return View(new ReporteAdminResumenActividadViewModel { Items = items });
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
