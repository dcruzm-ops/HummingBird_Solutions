namespace PSA.EntidadesDTO.DTOs.Pagos
{
    public class PlanPagoDTO
    {
        public int Id { get; set; }
        public int ConfiguracionPagoId { get; set; }
        public string NombrePlan { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public int CantidadCuotas { get; set; }
        public DateTime FechaInicio { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}