using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Fincas
{
    public class FincaEvidencia : BaseEntity
    {
        public int FincaId { get; set; }
        public string UrlArchivo { get; set; } = string.Empty;
        public string TipoArchivo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}