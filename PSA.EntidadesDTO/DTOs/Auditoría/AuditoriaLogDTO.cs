namespace PSA.EntidadesDTO.DTOs.Auditoria
{
    public class AuditoriaLogDTO
    {
        public int Id { get; set; }
        public int? UsuarioId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string Entidad { get; set; } = string.Empty;
        public int? EntidadId { get; set; }
        public string? Descripcion { get; set; }
        public string? DireccionIp { get; set; }
    }
}