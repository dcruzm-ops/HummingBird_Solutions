using System.Collections.Generic;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class RolPermisoDTO
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string? DescripcionRol { get; set; }
        public bool Activo { get; set; }
        public List<string> CodigosPermisoAsignados { get; set; } = new();
        public List<PermisoDTO> PermisosDisponibles { get; set; } = new();
    }
}
