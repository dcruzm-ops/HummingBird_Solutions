using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class ConfiguracionPagoAdminDTO
    {
        public int IdConfiguracionPago { get; set; }
        public int Version { get; set; }

        [Required(ErrorMessage = "El nombre de la versión es obligatorio.")]
        public string NombreVersion { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "El precio base no puede ser negativo.")]
        public decimal PrecioBasePorHectarea { get; set; }

        [Range(0, 100, ErrorMessage = "El tope de ajuste debe estar entre 0 y 100.")]
        public decimal TopePorcentajeAjuste { get; set; }

        [Required(ErrorMessage = "La fecha de vigencia inicial es obligatoria.")]
        public DateTime FechaVigenciaDesde { get; set; } = DateTime.Today;

        public DateTime? FechaVigenciaHasta { get; set; }
        public bool Activa { get; set; }
        public int CreadoPor { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<ConfiguracionPagoAjusteDTO> Ajustes { get; set; } = new();
    }
}
