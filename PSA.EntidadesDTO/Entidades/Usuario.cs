namespace PSA.EntidadesDTO.Entidades
{
	public class Usuario
	{
		public int IdUsuario { get; set; }

		public string Nombre { get; set; } = string.Empty;

		public string Apellidos { get; set; } = string.Empty;

		public string CorreoElectronico { get; set; } = string.Empty;

		public string? PasswordHash { get; set; }

		public int IdRol { get; set; }

		public bool Activo { get; set; } = true;

		public DateTime FechaCreacion { get; set; } = DateTime.Now;
	}
}