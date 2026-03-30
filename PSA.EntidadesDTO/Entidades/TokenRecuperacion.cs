namespace PSA.EntidadesDTO.Entidades
{
    public class TokenRecuperacion
    {
        public int IdToken { get; set; }
        public int IdUsuario { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public bool Usado { get; set; }
        public DateTime? FechaUso { get; set; }
    }
}
