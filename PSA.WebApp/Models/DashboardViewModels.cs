using PSA.EntidadesDTO.DTOs.Evaluaciones;

namespace PSA.WebApp.Models
{
    public class ActividadDashboardViewModel
    {
        public string Titulo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }

        // Se mantiene para no romper otras vistas que todavía usan links
        public string? Url { get; set; }
    }

    public class DashboardDuenoViewModel
    {
        public int FincasRegistradas { get; set; }
        public int EvaluacionesPendientes { get; set; }
        public int CuotasPorConfirmar { get; set; }
        public List<ActividadDashboardViewModel> ActividadReciente { get; set; } = new();
    }

    public class DashboardIngenieroViewModel
    {
        public int FincasPendientes { get; set; }
        public int EvaluacionesAbiertas { get; set; }
        public int DecisionesMesActual { get; set; }
        public List<BandejaEvaluacionPendienteDTO> ColaPendientesVisita { get; set; } = new();
        public List<ActividadDashboardViewModel> ProximasAcciones { get; set; } = new();
    }

    public class DashboardAdministradorViewModel
    {
        public int UsuariosActivos { get; set; }
        public int CuentasPorValidar { get; set; }
        public int EventosAuditoria24h { get; set; }
        public List<ActividadDashboardViewModel> Alertas { get; set; } = new();
    }

    // Déjalas por ahora si tu proyecto todavía las referencia o si Hot Reload no deja borrarlas
    public class DashboardDuenoApiModel
    {
        public int FincasRegistradas { get; set; }
        public int EvaluacionesPendientes { get; set; }
        public int CuotasPorConfirmar { get; set; }
    }

    public class DashboardIngenieroApiModel
    {
        public int FincasPendientes { get; set; }
        public int EvaluacionesAbiertas { get; set; }
        public int DecisionesMesActual { get; set; }
    }
}