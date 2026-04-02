using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Fincas
{
    public class FincaEvidencia : BaseEntity
    {
        public int FincaId { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaArchivo { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
        public int CargadoPor { get; set; }
    }
}