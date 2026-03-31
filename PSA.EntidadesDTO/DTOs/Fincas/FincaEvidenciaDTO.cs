namespace PSA.EntidadesDTO.DTOs.Fincas
{
    public class FincaEvidenciaDTO
    {
        public int Id { get; set; }
        public int FincaId { get; set; }
        public string UrlArchivo { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}