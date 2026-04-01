using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Evaluaciones
{
    public class EvaluacionTecnica : BaseEntity, IAuditable
    {
        public int FincaId { get; set; }
        public int IngenieroForestalId { get; set; }
        public DateTime FechaEvaluacion { get; set; }

        public string Estado { get; set; } = "Pendiente";

        public string Decision { get; set; } = "Pendiente"; 

        public string? Observaciones { get; set; }
        public decimal? Puntaje { get; set; }

        public int? CreadoPor { get; set; }
        public int? ActualizadoPor { get; set; }
    }
}