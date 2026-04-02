namespace PSA.EntidadesDTO.DTOs.Fincas
{
    public class FincaEvidenciaDTO
    {
        public int IdEvidencia { get; set; }
        public int FincaId { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaArchivo { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
        public int CargadoPor { get; set; }
        public string UrlDescarga { get; set; } = string.Empty;
    }
}