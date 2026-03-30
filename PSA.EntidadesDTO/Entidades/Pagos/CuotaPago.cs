using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Pagos
{
    public class CuotaPago : BaseEntity
    {
        public int PlanPagoId { get; set; }
        public int NumeroCuota { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public string Estado { get; set; } = "Pendiente";
    }
}