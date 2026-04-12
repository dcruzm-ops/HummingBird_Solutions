using PSA.EntidadesDTO.DTOs.Reportes;

namespace PSA.WebApp.Models;

public class ReportesIndexViewModel
{
    public bool EsAdmin { get; set; }
    public bool EsIngeniero { get; set; }
    public bool EsDueno { get; set; }
}

public class ReporteDuenoFincasViewModel
{
    public string? EstadoFinca { get; set; }
    public string? EstadoEvaluacion { get; set; }
    public List<ItemFincaDuenoReporteDTO> Items { get; set; } = new();
}

public class ReporteDuenoPagosViewModel
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }
    public string? EstadoCuota { get; set; }
    public ReportePagosDuenoDTO ReportePagos { get; set; } = new();
    public List<ItemTransaccionDuenoDTO> Transacciones { get; set; } = new();
}

public class ReporteIngenieroPendientesViewModel
{
    public string? Provincia { get; set; }
    public List<ItemFincaPendienteIngenieroDTO> Items { get; set; } = new();
}

public class ReporteIngenieroEvaluacionesViewModel
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }
    public string VistaPeriodo { get; set; } = "Mensual";
    public string? EstadoEvaluacion { get; set; }
    public ReporteEvaluacionesIngenieroDTO Reporte { get; set; } = new();
}

public class ReporteIngenieroTecnicoViewModel
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }
    public string? Decision { get; set; }
    public List<ItemTecnicoFincaDTO> Items { get; set; } = new();
}

public class ReporteAdminUsuariosViewModel
{
    public string? Rol { get; set; }
    public string? Estado { get; set; }
    public List<ItemUsuarioRolReporteDTO> Items { get; set; } = new();
}

public class ReporteAdminEvaluacionesViewModel
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }
    public string? Estado { get; set; }
    public string? Decision { get; set; }
    public List<ItemEvaluacionAdminDTO> Items { get; set; } = new();
}

public class ReporteAdminPagosViewModel
{
    public int? AnioPlanes { get; set; }
    public int? AnioUbicacion { get; set; }
    public string? EstadoCuotas { get; set; }
    public string? Provincia { get; set; }
    public string? Canton { get; set; }
    public string? Distrito { get; set; }
    public List<ItemPagosAdminDTO> Planes { get; set; } = new();
    public List<ItemPagoUbicacionDTO> PagosPorUbicacion { get; set; } = new();
}

public class ReporteAdminAuditoriaViewModel
{
    public string? Modulo { get; set; }
    public string? Accion { get; set; }
    public int Top { get; set; } = 50;
    public List<ItemAuditoriaCriticaDTO> Items { get; set; } = new();
}

public class ReporteAdminFincasEstadoViewModel
{
    public List<ItemFincaEstadoAdminDTO> Items { get; set; } = new();
}

public class ReporteAdminResumenActividadViewModel
{
    public List<ItemResumenActividadDTO> Items { get; set; } = new();
}
