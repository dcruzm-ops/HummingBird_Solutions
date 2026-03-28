namespace PSA.EntidadesDTO.DTOs.Usuarios
{
    public class TokenRecuperacionDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
        public bool Usado { get; set; }
    }
}