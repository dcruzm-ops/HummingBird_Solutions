namespace PSA.EntidadesDTO.DTOs.Evaluaciones
{
    public class EvaluacionEvidenciaDTO
    {
        public int IdEvidenciaEvaluacion { get; set; }
        public int IdEvaluacion { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaArchivo { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
        public int CargadoPor { get; set; }
        public string UrlDescarga { get; set; } = string.Empty;
    }
}
