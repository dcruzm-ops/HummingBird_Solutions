using System;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class UsuarioAdminListadoDTO
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public int CantidadFincas { get; set; }
        public int CantidadEvaluacionesActivas { get; set; }
    }
}
