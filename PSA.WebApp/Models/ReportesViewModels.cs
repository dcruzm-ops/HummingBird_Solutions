using PSA.EntidadesDTO.DTOs.Reportes;

namespace PSA.WebApp.Models;

public class ReportesIndexViewModel
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }

    public ReportePagosDuenoDTO PagosDueno { get; set; } = new();
    public List<ItemTransaccionDuenoDTO> TransaccionesDueno { get; set; } = new();

    public ReporteEvaluacionesIngenieroDTO EvaluacionesIngeniero { get; set; } = new();

    public List<ItemPagoUbicacionDTO> PagosPorUbicacion { get; set; } = new();
    public List<ItemResumenActividadDTO> ResumenActividad { get; set; } = new();
}
