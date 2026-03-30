using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Usuarios
{
    public class Usuario : BaseEntity, IAuditable
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string ContrasenaHash { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public int RolId { get; set; }
        public bool CorreoVerificado { get; set; } = false;

        public int? CreadoPor { get; set; }
        public int? ActualizadoPor { get; set; }
    }
}