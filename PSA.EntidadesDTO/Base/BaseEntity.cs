namespace PSA.EntidadesDTO.Base
{
    public abstract class BaseEntity : IBaseEntity
    {
        public int Id { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
        public bool Activo { get; set; } = true;
    }
}