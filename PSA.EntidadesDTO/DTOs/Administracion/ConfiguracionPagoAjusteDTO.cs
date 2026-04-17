using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class ConfiguracionPagoAjusteDTO
    {
        public int IdDetalleConfiguracion { get; set; }
        public string TipoFactor { get; set; } = string.Empty;
        public string ValorFactor { get; set; } = string.Empty;

        [Range(-100, 100, ErrorMessage = "El porcentaje debe estar entre -100 y 100.")]
        public decimal PorcentajeAjuste { get; set; }
    }
}
