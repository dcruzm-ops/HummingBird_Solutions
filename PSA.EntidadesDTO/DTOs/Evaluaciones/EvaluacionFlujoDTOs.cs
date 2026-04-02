namespace PSA.EntidadesDTO.DTOs.Evaluaciones
{
    public static class EstadosEvaluacionTecnica
    {
        public const string Pendiente = "Pendiente";
        public const string EnProceso = "En proceso";
        public const string EvaluadaNoCalifica = "Evaluada – No califica";
        public const string EvaluadaCalifica = "Evaluada – Califica";
        public const string PendienteCuentaBancaria = "Pendiente de cuenta bancaria";
        public const string PendienteAprobacionFinalPago = "Pendiente de aprobación final de pago";
        public const string PagosActivos = "Pagos activos";

        public static readonly HashSet<string> Todos = new(StringComparer.OrdinalIgnoreCase)
        {
            Pendiente,
            EnProceso,
            EvaluadaNoCalifica,
            EvaluadaCalifica,
            PendienteCuentaBancaria,
            PendienteAprobacionFinalPago,
            PagosActivos
        };
    }

    public class BandejaEvaluacionPendienteDTO
    {
        public int IdEvaluacion { get; set; }
        public int IdFinca { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Canton { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
        public decimal Hectareas { get; set; }
        public string EstadoEvaluacion { get; set; } = EstadosEvaluacionTecnica.Pendiente;
        public int? IdIngeniero { get; set; }
    }

    public class AsignarEvaluacionDTO
    {
        public int IdIngeniero { get; set; }
    }

    public class RegistrarResultadoEvaluacionDTO
    {
        public DateTime FechaVisita { get; set; }
        public string? Observaciones { get; set; }
        public string DecisionTecnica { get; set; } = string.Empty;
        public decimal? HectareasAjustadas { get; set; }
        public string? VegetacionAjustada { get; set; }
        public bool? RecursosHidricosAjustado { get; set; }
        public string? UsoSueloAjustado { get; set; }
        public string? PendienteAjustada { get; set; }
    }

    public class DetalleFincaParaEvaluacionDTO
    {
        public int IdEvaluacion { get; set; }
        public int IdFinca { get; set; }
        public int IdPropietario { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Canton { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
        public decimal Hectareas { get; set; }
        public string Vegetacion { get; set; } = string.Empty;
        public bool TieneRecursosHidricos { get; set; }
        public string UsoSuelo { get; set; } = string.Empty;
        public string Pendiente { get; set; } = string.Empty;
        public string EstadoFinca { get; set; } = string.Empty;
        public string EstadoEvaluacion { get; set; } = string.Empty;
        public int? IdIngeniero { get; set; }
    }

    public class FiltroReporteEvaluacionesDTO
    {
        public int? Anio { get; set; }
        public int? Mes { get; set; }
        public string? EstadoEvaluacion { get; set; }
        public string? DecisionTecnica { get; set; }
        public int? IdIngeniero { get; set; }
    }

    public class ItemReporteEvaluacionDTO
    {
        public int IdEvaluacion { get; set; }
        public int IdFinca { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public string EstadoEvaluacion { get; set; } = string.Empty;
        public string? DecisionTecnica { get; set; }
        public DateTime? FechaVisita { get; set; }
        public DateTime? FechaDecision { get; set; }
        public string Provincia { get; set; } = string.Empty;
        public string Canton { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
    }

    public class ReporteEvaluacionesDTO
    {
        public int TotalEvaluaciones { get; set; }
        public int TotalCalifica { get; set; }
        public int TotalNoCalifica { get; set; }
        public int TotalPendientes { get; set; }
        public List<ItemReporteEvaluacionDTO> Evaluaciones { get; set; } = new();
    }
}
