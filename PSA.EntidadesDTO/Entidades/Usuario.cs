namespace PSA.EntidadesDTO.Entidades
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PasswordHash { get; set; }

        public int IdRol { get; set; }

        public string Estado { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        public DateTime? UltimoAcceso { get; set; }
    }
}