using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Usuarios
{
    public class TokenRecuperacion : BaseEntity
    {
        public int UsuarioId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
        public bool Usado { get; set; } = false;
    }
}