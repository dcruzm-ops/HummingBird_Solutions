using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;

namespace PSA.WebApp.Models
{
    public class GestionUsuariosViewModel
    {
        public List<UsuarioAdminListadoDTO> Usuarios { get; set; } = new();
    }

    public class FormularioUsuarioAdminViewModel
    {
        public UsuarioAdminEdicionDTO Usuario { get; set; } = new();
        public List<RolDTO> Roles { get; set; } = new();
        public bool EsEdicion => Usuario?.IdUsuario > 0;
        public string TituloFormulario { get; set; } = string.Empty;
        public string TextoAccion { get; set; } = string.Empty;
    }

    public class ReasignacionClienteViewModel
    {
        public ReasignacionClienteDTO Reasignacion { get; set; } = new();
        public List<UsuarioAdminListadoDTO> Propietarios { get; set; } = new();
        public List<UsuarioAdminListadoDTO> Ingenieros { get; set; } = new();
    }

    public class RolesPermisosViewModel
    {
        public List<RolPermisoDTO> Roles { get; set; } = new();
        public List<PermisoDTO> PermisosDisponibles { get; set; } = new();
        public CrearRolDTO NuevoRol { get; set; } = new();
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
        public AuditoriaOpcionesFiltroDTO OpcionesFiltro { get; set; } = new();
        public List<AuditoriaEventoDTO> Eventos { get; set; } = new();
    }
}
