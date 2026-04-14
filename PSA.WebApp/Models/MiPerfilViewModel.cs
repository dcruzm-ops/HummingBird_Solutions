using System.ComponentModel.DataAnnotations;

namespace PSA.WebApp.Models
{
    public class MiPerfilViewModel
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        public string RolNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        public string Iniciales
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NombreCompleto)) return "U";
                var partes = NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length == 1) return partes[0][0].ToString().ToUpperInvariant();
                return string.Concat(partes[0][0], partes[^1][0]).ToUpperInvariant();
            }
        }
    }
}
