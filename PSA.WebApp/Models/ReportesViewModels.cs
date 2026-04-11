using PSA.EntidadesDTO.DTOs.Reportes;

namespace PSA.WebApp.Models;

public class ReportesIndexViewModel
{
    public int? Anio { get; set; }
    public int? Mes { get; set; }

    public ReportePagosDuenoDTO PagosDueno { get; set; } = new();
    public List<ItemTransaccionDuenoDTO> TransaccionesDueno { get; set; } = new();
    public List<ItemFincaDuenoReporteDTO> FincasDueno { get; set; } = new();

    public ReporteEvaluacionesIngenieroDTO EvaluacionesIngeniero { get; set; } = new();
    public List<ItemFincaPendienteIngenieroDTO> FincasPendientesIngeniero { get; set; } = new();
    public List<ItemTecnicoFincaDTO> ReporteTecnicoFinca { get; set; } = new();

    public List<ItemPagoUbicacionDTO> PagosPorUbicacion { get; set; } = new();
    public List<ItemResumenActividadDTO> ResumenActividad { get; set; } = new();
    public List<ItemUsuarioRolReporteDTO> UsuariosRoles { get; set; } = new();
    public List<ItemFincaEstadoAdminDTO> FincasPorEstado { get; set; } = new();
    public List<ItemEvaluacionAdminDTO> EvaluacionesAdmin { get; set; } = new();
    public List<ItemPagosAdminDTO> PagosAdmin { get; set; } = new();
    public List<ItemAuditoriaCriticaDTO> AuditoriaCritica { get; set; } = new();
}
