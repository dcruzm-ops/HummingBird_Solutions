namespace PSA.EntidadesDTO.DTOs.Fincas
{
    public class FincaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public decimal ExtensionHectareas { get; set; }
        public string? TipoBosque { get; set; }
        public string? Descripcion { get; set; }
        public int PropietarioId { get; set; }
    }
}