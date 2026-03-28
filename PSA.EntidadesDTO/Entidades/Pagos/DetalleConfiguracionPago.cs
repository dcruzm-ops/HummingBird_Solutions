using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Pagos
{
    public class DetalleConfiguracionPago : BaseEntity
    {
        public int ConfiguracionPagoId { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Periodicidad { get; set; } = string.Empty;
    }
}