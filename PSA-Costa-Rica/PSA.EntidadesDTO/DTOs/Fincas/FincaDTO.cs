using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs.Fincas
{
    public class FincaDTO
    {
        public int IdFinca { get; set; }

        [Required(ErrorMessage = "El propietario es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un propietario válido.")]
        public int IdPropietario { get; set; }

        [Required(ErrorMessage = "El nombre de la finca es obligatorio.")]
        [StringLength(100)]
        public string NombreFinca { get; set; } = string.Empty;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        public string Provincia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cantón es obligatorio.")]
        public string Canton { get; set; } = string.Empty;

        [Required(ErrorMessage = "El distrito es obligatorio.")]
        public string Distrito { get; set; } = string.Empty;

        public string? DireccionExacta { get; set; }

        [Range(-90, 90, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
        public decimal Latitud { get; set; }

        [Range(-180, 180, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
        public decimal Longitud { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Las hectáreas deben ser mayores a 0.")]
        public decimal Hectareas { get; set; }

        [Required(ErrorMessage = "La vegetación es obligatoria.")]
        public string Vegetacion { get; set; } = string.Empty;

        public bool TieneRecursosHidricos { get; set; }

        [Required(ErrorMessage = "El uso de suelo es obligatorio.")]
        public string UsoSuelo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La pendiente es obligatoria.")]
        public string Pendiente { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado de la finca es obligatorio.")]
        public string EstadoFinca { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}