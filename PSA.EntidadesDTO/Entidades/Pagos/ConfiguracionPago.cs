using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Pagos
{
    public class ConfiguracionPago : BaseEntity, IAuditable
    {
        public int FincaId { get; set; }
        public int CuentaBancariaId { get; set; }
        public string Estado { get; set; } = "Activa";
        public string? Observaciones { get; set; }

        public int? CreadoPor { get; set; }
        public int? ActualizadoPor { get; set; }
    }
}