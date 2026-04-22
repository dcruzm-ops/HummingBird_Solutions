using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;

namespace PSA.WebApp.Models
{
    public class GestionUsuariosViewModel
    {
        public List<UsuarioAdminListadoDTO> Usuarios { get; set; } = new();
    }

    public class ParametrosPagoViewModel
    {
        public ConfiguracionPagoAdminDTO NuevaConfiguracion { get; set; } = new();
        public ConfiguracionPagoAdminDTO? ConfiguracionActual { get; set; }
        public List<ConfiguracionPagoAdminDTO> Historial { get; set; } = new();
    }

    public class ValidacionCuentasBancariasViewModel
    {
        public List<CuentaBancariaPendienteDTO> CuentasPendientes { get; set; } = new();
    }

    public class AuditoriaLogsViewModel
    {
        public AuditoriaFiltroDTO Filtro { get; set; } = new();
        public List<AuditoriaEventoDTO> Eventos { get; set; } = new();
    }
}
