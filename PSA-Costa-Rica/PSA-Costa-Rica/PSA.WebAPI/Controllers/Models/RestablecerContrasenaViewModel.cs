using System.ComponentModel.DataAnnotations;

namespace PSA.WebApp.Models
{
    public class RestablecerContrasenaViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required]
        [Compare("NuevaContrasena")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}