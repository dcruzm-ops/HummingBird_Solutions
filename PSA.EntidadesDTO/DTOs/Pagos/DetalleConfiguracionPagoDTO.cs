namespace PSA.EntidadesDTO.DTOs.Pagos
{
    public class DetalleConfiguracionPagoDTO
    {
        public int Id { get; set; }
        public int ConfiguracionPagoId { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Periodicidad { get; set; } = string.Empty;
    }
}