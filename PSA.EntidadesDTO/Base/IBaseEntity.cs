namespace PSA.EntidadesDTO.Base
{
    public interface IBaseEntity
    {
        int Id { get; set; }
        DateTime FechaCreacion { get; set; }
        DateTime? FechaActualizacion { get; set; }
        bool Activo { get; set; }
    }
}
