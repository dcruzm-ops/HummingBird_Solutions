using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Usuarios
{
    public class Rol : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}