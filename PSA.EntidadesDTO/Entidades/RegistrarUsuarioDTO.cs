namespace PSA.EntidadesDTO.DTOs
{
    public class RegistrarUsuarioDTO
    {
        public string Nombre { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string CorreoElectronico { get; set; } = string.Empty;

        public string Contrasena { get; set; } = string.Empty;

        public string ConfirmacionContrasena { get; set; } = string.Empty;

        public int IdRol { get; set; }
    }
}