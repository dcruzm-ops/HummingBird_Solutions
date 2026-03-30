namespace PSA.EntidadesDTO.DTOs.Pagos
{
    public class CuotaPagoDTO
    {
        public int Id { get; set; }
        public int PlanPagoId { get; set; }
        public int NumeroCuota { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}