using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class UsuarioAdminEdicionDTO
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un rol válido.")]
        public int IdRol { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        public string Estado { get; set; } = "Activo";

        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string? Contrasena { get; set; }

        [Compare("Contrasena", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string? ConfirmacionContrasena { get; set; }
    }
}
