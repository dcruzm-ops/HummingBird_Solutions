using System.ComponentModel.DataAnnotations;

namespace PSA.WebApp.Models.Fincas
{
    public class FincaViewModel
    {
        public int IdFinca { get; set; }

        [Required(ErrorMessage = "El propietario es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un propietario válido.")]
        public int IdPropietario { get; set; }

        [Required(ErrorMessage = "El nombre de la finca es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre de la finca no puede superar los 100 caracteres.")]
        public string NombreFinca { get; set; } = string.Empty;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(50, ErrorMessage = "La provincia no puede superar los 50 caracteres.")]
        public string Provincia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cantón es obligatorio.")]
        [StringLength(50, ErrorMessage = "El cantón no puede superar los 50 caracteres.")]
        public string Canton { get; set; } = string.Empty;

        [Required(ErrorMessage = "El distrito es obligatorio.")]
        [StringLength(50, ErrorMessage = "El distrito no puede superar los 50 caracteres.")]
        public string Distrito { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La dirección exacta no puede superar los 250 caracteres.")]
        public string? DireccionExacta { get; set; }

        [Range(-90, 90, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
        public decimal Latitud { get; set; }

        [Range(-180, 180, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
        public decimal Longitud { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Las hectáreas deben ser un número positivo mayor a 0.")]
        public decimal Hectareas { get; set; }

        [Required(ErrorMessage = "La vegetación es obligatoria.")]
        [StringLength(100, ErrorMessage = "La vegetación no puede superar los 100 caracteres.")]
        public string Vegetacion { get; set; } = string.Empty;

        public bool TieneRecursosHidricos { get; set; }

        [Required(ErrorMessage = "El uso de suelo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El uso de suelo no puede superar los 100 caracteres.")]
        public string UsoSuelo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La pendiente es obligatoria.")]
        [StringLength(50, ErrorMessage = "La pendiente no puede superar los 50 caracteres.")]
        public string Pendiente { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado de la finca es obligatorio.")]
        [StringLength(50, ErrorMessage = "El estado de la finca no puede superar los 50 caracteres.")]
        public string EstadoFinca { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }
        public DateTime FechaActualizacion { get; set; }

        public string? CodigoExpediente { get; set; }
        public string? Descripcion { get; set; }
    }
}