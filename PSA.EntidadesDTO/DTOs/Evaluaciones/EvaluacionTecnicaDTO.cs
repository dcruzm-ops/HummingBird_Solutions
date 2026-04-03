namespace PSA.EntidadesDTO.DTOs.Evaluaciones
{
    public class EvaluacionTecnicaDTO
    {
        public int Id { get; set; }
        public int FincaId { get; set; }
        public int IngenieroForestalId { get; set; }
        public DateTime FechaEvaluacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public decimal? Puntaje { get; set; }
    }
}