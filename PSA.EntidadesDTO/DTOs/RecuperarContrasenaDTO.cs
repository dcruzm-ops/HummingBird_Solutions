using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs
{
    public class RecuperarContrasenaDTO
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Email { get; set; } = string.Empty;

        // Compatibilidad con llamadas que aún usan la propiedad Correo
        public string Correo
        {
            get => Email;
            set => Email = value;
        }
    }
}
