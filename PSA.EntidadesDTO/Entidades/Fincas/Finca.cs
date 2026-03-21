using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Fincas
{
    public class Finca : BaseEntity, IAuditable
    {
        public string Nombre { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public decimal ExtensionHectareas { get; set; }
        public string? TipoBosque { get; set; }
        public string? Descripcion { get; set; }
        public int PropietarioId { get; set; }

        public int? CreadoPor { get; set; }
        public int? ActualizadoPor { get; set; }
    }
}