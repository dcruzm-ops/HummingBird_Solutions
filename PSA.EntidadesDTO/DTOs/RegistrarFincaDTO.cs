using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs
{
    public class RegistrarFincaDTO
    {
        public int IdPropietario { get; set; }

        [Required(ErrorMessage = "El nombre de la finca es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
        [Display(Name = "Nombre de la Finca")]
        public string NombreFinca { get; set; } = string.Empty;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(100, ErrorMessage = "La provincia no puede superar 100 caracteres.")]
        [Display(Name = "Provincia")]
        public string Provincia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cantón es obligatorio.")]
        [StringLength(100, ErrorMessage = "El cantón no puede superar 100 caracteres.")]
        [Display(Name = "Cantón")]
        public string Canton { get; set; } = string.Empty;

        [Required(ErrorMessage = "El distrito es obligatorio.")]
        [StringLength(100, ErrorMessage = "El distrito no puede superar 100 caracteres.")]
        [Display(Name = "Distrito")]
        public string Distrito { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La dirección exacta no puede superar 250 caracteres.")]
        [Display(Name = "Dirección Exacta")]
        public string? DireccionExacta { get; set; }

        [Required(ErrorMessage = "La latitud es obligatoria.")]
        [Range(-90, 90, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
        public decimal Latitud { get; set; }

        [Required(ErrorMessage = "La longitud es obligatoria.")]
        [Range(-180, 180, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
        public decimal Longitud { get; set; }

        [Required(ErrorMessage = "Las hectáreas son obligatorias.")]
        [Range(0.01, 100000, ErrorMessage = "Hectáreas debe ser mayor a 0.")]
        [Display(Name = "Tamaño en Hectáreas")]
        public decimal Hectareas { get; set; }

        [Required(ErrorMessage = "La vegetación es obligatoria.")]
        [StringLength(100, ErrorMessage = "La vegetación no puede superar 100 caracteres.")]
        [Display(Name = "Tipo de Vegetación")]
        public string Vegetacion { get; set; } = "Bosque primario";

        public bool TieneRecursosHidricos { get; set; }

        public bool TieneRiosOQuebradas { get; set; }

        public bool TieneNacientes { get; set; }

        [Range(0, 100, ErrorMessage = "La cantidad de nacientes debe estar entre 0 y 100.")]
        public int CantidadNacientes { get; set; }

        [Required(ErrorMessage = "El uso de suelo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El uso de suelo no puede superar 100 caracteres.")]
        [Display(Name = "Uso del Suelo")]
        public string UsoSuelo { get; set; } = "Conservación";

        [Required(ErrorMessage = "La pendiente es obligatoria.")]
        [StringLength(50, ErrorMessage = "La pendiente no puede superar 50 caracteres.")]
        [Display(Name = "Tipo de Superficie")]
        public string Pendiente { get; set; } = "Plana";
    }
}
