namespace PSA.EntidadesDTO.DTOs
{
    public class RestablecerContrasenaDTO
    {
        public string Token { get; set; } = string.Empty;
        public string NuevaContrasena { get; set; } = string.Empty;
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}