namespace PSA.EntidadesDTO.DTOs.Dashboard
{
    public class ResumenDashboardAdministradorDTO
    {
        public int UsuariosActivos { get; set; }
        public int UsuariosPendientesAprobacion { get; set; }
        public int CuentasPorValidar { get; set; }
        public int EventosAuditoria24h { get; set; }
        public List<string> Alertas { get; set; } = new();
    }
}
