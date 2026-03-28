using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Auditoria
{
    public class AuditoriaLog : BaseEntity
    {
        public int? UsuarioId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string Entidad { get; set; } = string.Empty;
        public int? EntidadId { get; set; }
        public string? Descripcion { get; set; }
        public string? DireccionIp { get; set; }
    }
}