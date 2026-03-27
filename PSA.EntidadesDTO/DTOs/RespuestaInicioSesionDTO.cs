namespace PSA.EntidadesDTO.DTOs
{
    public class RespuestaInicioSesionDTO
    {
        public int IdUsuario { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int IdRol { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        public string Mensaje { get; set; } = string.Empty;
    }
}
