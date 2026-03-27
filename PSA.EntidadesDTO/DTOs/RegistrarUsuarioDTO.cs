using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs
{
    public class RegistrarUsuarioDTO
    {
        [Required(ErrorMessage = "El nombre completo es requerido.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
        [Compare("Contrasena", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string ConfirmacionContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es requerido.")]
        [Range(1, int.MaxValue, ErrorMessage = "El IdRol debe ser mayor a 0.")]
        public int IdRol { get; set; }
    }
}
