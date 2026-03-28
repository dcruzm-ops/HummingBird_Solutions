using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Notificaciones
{
    public class Notificacion : BaseEntity
    {
        public int UsuarioId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool Leida { get; set; } = false;
        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
        public string? Tipo { get; set; }
    }
}