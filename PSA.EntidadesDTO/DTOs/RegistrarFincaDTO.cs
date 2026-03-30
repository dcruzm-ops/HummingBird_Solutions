using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs
{
    public class RegistrarFincaDTO
    {
        public int IdPropietario { get; set; }

        [Required(ErrorMessage = "El nombre de la finca es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
        public string NombreFinca { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Provincia { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Canton { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Distrito { get; set; } = string.Empty;

        [StringLength(250)]
        public string? DireccionExacta { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitud fuera de rango.")]
        public decimal Latitud { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitud fuera de rango.")]
        public decimal Longitud { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Hectáreas debe ser mayor a 0.")]
        public decimal Hectareas { get; set; }

        [Required]
        [StringLength(100)]
        public string Vegetacion { get; set; } = "Bosque";

        public bool TieneRecursosHidricos { get; set; }

        [Required]
        [StringLength(100)]
        public string UsoSuelo { get; set; } = "Conservación";

        [Required]
        [StringLength(50)]
        public string Pendiente { get; set; } = "Media";
    }
}
