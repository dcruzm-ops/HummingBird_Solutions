namespace PSA.EntidadesDTO.DTOs.RecuperacionContrasena
{
    public class RespuestaRecuperacionDTO
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string? LinkRecuperacion { get; set; }
        public string? CorreoDestino { get; set; }
        public string? NombreUsuario { get; set; }
    }
}