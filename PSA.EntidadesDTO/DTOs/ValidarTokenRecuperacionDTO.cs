using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs
{
    public class ValidarTokenRecuperacionDTO
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El token es obligatorio.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El token debe tener 6 dígitos.")]
        public string Token { get; set; } = string.Empty;

        public string Correo
        {
            get => Email;
            set => Email = value;
        }
    }
}
