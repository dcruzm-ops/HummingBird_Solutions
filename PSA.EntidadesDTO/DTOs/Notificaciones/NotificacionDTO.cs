namespace PSA.EntidadesDTO.DTOs.Notificaciones
{
    public class NotificacionDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool Leida { get; set; }
        public DateTime FechaEnvio { get; set; }
        public string? Tipo { get; set; }
    }
}