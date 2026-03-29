using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs
{
    public class RestablecerContrasenaDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
