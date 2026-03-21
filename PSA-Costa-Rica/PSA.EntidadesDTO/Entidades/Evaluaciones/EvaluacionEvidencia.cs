using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Evaluaciones
{
    public class EvaluacionEvidencia : BaseEntity
    {
        public int EvaluacionTecnicaId { get; set; }
        public string UrlArchivo { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}