namespace PSA.EntidadesDTO.DTOs.Dashboard
{
    public class ResumenDashboardAdministradorDTO
    {
        public int UsuariosActivos { get; set; }
        public int UsuariosNuevosHoy { get; set; }
        public int UsuariosPendientesAprobacion { get; set; }
        public int CuentasPorValidar { get; set; }
        public int EventosAuditoria24h { get; set; }
        public List<string> Alertas { get; set; } = new();
        public List<ActividadAuditoriaDTO> ActividadAuditoria { get; set; } = new();
    }

    public class ActividadAuditoriaDTO
    {
        public string Modulo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public DateTime FechaAccion { get; set; }
    }
}
