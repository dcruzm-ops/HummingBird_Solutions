namespace PSA.EntidadesDTO.DTOs.Reportes;

public class FiltroReporteDTO
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }
}

public class ItemPagoMensualDuenoDTO
{
    public int IdPlanPago { get; set; }
    public int IdFinca { get; set; }
    public string NombreFinca { get; set; } = string.Empty;
    public int Anio { get; set; }
    public int Mes { get; set; }
    public DateTime FechaProgramada { get; set; }
    public decimal MontoBaseMensual { get; set; }
    public decimal PorcentajeAjusteTotal { get; set; }
    public decimal MontoMensualCalculado { get; set; }
    public decimal MontoPendiente { get; set; }
    public string EstadoCuota { get; set; } = string.Empty;
}

public class ItemAjustePagoDTO
{
    public int IdPlanPago { get; set; }
    public string TipoFactor { get; set; } = string.Empty;
    public string ValorFactor { get; set; } = string.Empty;
    public decimal PorcentajeAjuste { get; set; }
}

public class ReportePagosDuenoDTO
{
    public List<ItemPagoMensualDuenoDTO> PagosMensuales { get; set; } = new();
    public List<ItemAjustePagoDTO> AjustesAplicados { get; set; } = new();
}

public class ItemTransaccionDuenoDTO
{
    public int IdTransaccionPago { get; set; }
    public DateTime FechaTransaccion { get; set; }
    public decimal MontoTotal { get; set; }
    public string EstadoTransaccion { get; set; } = string.Empty;
    public string? ReferenciaExterna { get; set; }
    public string? Observaciones { get; set; }
    public int IdPlanPago { get; set; }
    public int IdFinca { get; set; }
    public string NombreFinca { get; set; } = string.Empty;
}

public class ItemEvaluacionIngenieroDTO
{
    public int IdEvaluacion { get; set; }
    public int? IdIngeniero { get; set; }
    public string Ingeniero { get; set; } = string.Empty;
    public string EstadoEvaluacion { get; set; } = string.Empty;
    public string? DecisionTecnica { get; set; }
    public DateTime? FechaVisita { get; set; }
    public DateTime? FechaDecision { get; set; }
    public int IdFinca { get; set; }
    public string NombreFinca { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string Canton { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
}

public class ReporteEvaluacionesIngenieroDTO
{
    public int Total { get; set; }
    public int TotalCalifica { get; set; }
    public int TotalNoCalifica { get; set; }
    public List<ItemEvaluacionIngenieroDTO> Evaluaciones { get; set; } = new();
}

public class ItemPagoUbicacionDTO
{
    public string Provincia { get; set; } = string.Empty;
    public string Canton { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal MontoPagadoMes { get; set; }
    public decimal MontoProgramadoMes { get; set; }
}

public class ItemResumenActividadDTO
{
    public string Indicador { get; set; } = string.Empty;
    public long Total { get; set; }
}

public class ItemFincaDuenoReporteDTO
{
    public int IdFinca { get; set; }
    public string NombreFinca { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string Canton { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
    public string EstadoFinca { get; set; } = string.Empty;
    public string EstadoEvaluacion { get; set; } = string.Empty;
    public string? ObservacionesEvaluacion { get; set; }
}

public class ItemFincaPendienteIngenieroDTO
{
    public int IdEvaluacion { get; set; }
    public int IdFinca { get; set; }
    public string NombreFinca { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string Canton { get; set; } = string.Empty;
    public string Distrito { get; set; } = string.Empty;
    public string EstadoEvaluacion { get; set; } = string.Empty;
}

public class ItemTecnicoFincaDTO
{
    public int IdEvaluacion { get; set; }
    public int IdFinca { get; set; }
    public string NombreFinca { get; set; } = string.Empty;
    public string EstadoEvaluacion { get; set; } = string.Empty;
    public DateTime? FechaVisita { get; set; }
    public string? Observaciones { get; set; }
    public string? DecisionTecnica { get; set; }
    public decimal? HectareasAjustadas { get; set; }
    public string? VegetacionAjustada { get; set; }
    public bool? RecursosHidricosAjustado { get; set; }
    public string? UsoSueloAjustado { get; set; }
    public string? PendienteAjustada { get; set; }
}

public class ItemUsuarioRolReporteDTO
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class ItemFincaEstadoAdminDTO
{
    public string EstadoFinca { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class ItemEvaluacionAdminDTO
{
    public int IdEvaluacion { get; set; }
    public string NombreFinca { get; set; } = string.Empty;
    public string Ingeniero { get; set; } = string.Empty;
    public DateTime? FechaVisita { get; set; }
    public DateTime? FechaDecision { get; set; }
    public string EstadoEvaluacion { get; set; } = string.Empty;
    public string? DecisionTecnica { get; set; }
    public string? Observaciones { get; set; }
}

public class ItemPagosAdminDTO
{
    public int IdPlanPago { get; set; }
    public string NombreFinca { get; set; } = string.Empty;
    public int Anio { get; set; }
    public int CuotasPendientes { get; set; }
    public int CuotasPagadas { get; set; }
}

public class ItemAuditoriaCriticaDTO
{
    public DateTime FechaAccion { get; set; }
    public string Modulo { get; set; } = string.Empty;
    public string TablaAfectada { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string Usuario { get; set; } = string.Empty;
}
