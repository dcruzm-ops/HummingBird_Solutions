namespace PSA.EntidadesDTO.DTOs.Pagos
{
    public class ConfiguracionPagoDTO
    {
        public int Id { get; set; }
        public int FincaId { get; set; }
        public int CuentaBancariaId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}