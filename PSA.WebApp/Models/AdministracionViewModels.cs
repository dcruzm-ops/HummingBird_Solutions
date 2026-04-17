using System.Collections.Generic;
using PSA.EntidadesDTO.DTOs.Administracion;
using PSA.EntidadesDTO.DTOs.Usuarios;

namespace PSA.WebApp.Models
{
    public class FormularioUsuarioAdminViewModel
    {
        public UsuarioAdminEdicionDTO Usuario { get; set; } = new();
        public List<RolDTO> Roles { get; set; } = new();
        public bool EsEdicion => Usuario.IdUsuario > 0;
    }

    public class RolesPermisosViewModel
    {
        public List<RolPermisoDTO> Roles { get; set; } = new();
        public List<PermisoDTO> PermisosDisponibles { get; set; } = new();
    }
}
