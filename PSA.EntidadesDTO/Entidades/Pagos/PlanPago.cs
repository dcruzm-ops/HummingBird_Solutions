using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Pagos
{
    public class PlanPago : BaseEntity, IAuditable
    {
        public int ConfiguracionPagoId { get; set; }
        public string NombrePlan { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public int CantidadCuotas { get; set; }
        public DateTime FechaInicio { get; set; }
        public string Estado { get; set; } = "Pendiente";

        public int? CreadoPor { get; set; }
        public int? ActualizadoPor { get; set; }
    }
}