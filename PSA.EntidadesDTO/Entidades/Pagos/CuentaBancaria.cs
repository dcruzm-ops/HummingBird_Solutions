using PSA.EntidadesDTO.Base;

namespace PSA.EntidadesDTO.Entidades.Pagos
{
    public class CuentaBancaria : BaseEntity, IAuditable
    {
        public int UsuarioId { get; set; }
        public string Banco { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string Moneda { get; set; } = "CRC";
        public bool EsPrincipal { get; set; } = false;

        public int? CreadoPor { get; set; }
        public int? ActualizadoPor { get; set; }
    }
}