using PSA.EntidadesDTO.DTOs.Evaluaciones;

namespace PSA.WebApp.Models
{
    public class BandejaEvaluacionesViewModel
    {
        public List<BandejaEvaluacionPendienteDTO> Pendientes { get; set; } = new();
        public string EstadoFiltro { get; set; } = "Todos";
    }

    public class NuevaEvaluacionViewModel
    {
        public DetalleFincaParaEvaluacionDTO Detalle { get; set; } = new();
        public RegistrarResultadoEvaluacionDTO Formulario { get; set; } = new();
    }

    public class ReporteEvaluacionesViewModel
    {
        public int? Anio { get; set; }
        public int? Mes { get; set; }
        public string? EstadoEvaluacion { get; set; }
        public string? DecisionTecnica { get; set; }
        public ReporteEvaluacionesDTO Reporte { get; set; } = new();
    }
}
