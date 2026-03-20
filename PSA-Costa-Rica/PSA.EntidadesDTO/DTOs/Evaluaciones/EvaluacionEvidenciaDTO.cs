namespace PSA.EntidadesDTO.DTOs.Evaluaciones
{
    public class EvaluacionEvidenciaDTO
    {
        public int Id { get; set; }
        public int EvaluacionTecnicaId { get; set; }
        public string UrlArchivo { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}