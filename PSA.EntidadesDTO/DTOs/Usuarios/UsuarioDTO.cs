namespace PSA.EntidadesDTO.DTOs.Usuarios
{
    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public int RolId { get; set; }
        public bool CorreoVerificado { get; set; }
    }
}