using System.ComponentModel.DataAnnotations;

namespace PSA.WebApp.Models
{
    public class RecuperarContrasenaViewModel
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;
    }
}
