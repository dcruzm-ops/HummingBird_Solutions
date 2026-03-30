using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs
{
    public class ValidarTokenRecuperacionDTO
    {
        [Required(ErrorMessage = "El token es obligatorio.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El token debe tener 6 dígitos.")]
        public string Token { get; set; } = string.Empty;
    }
}
