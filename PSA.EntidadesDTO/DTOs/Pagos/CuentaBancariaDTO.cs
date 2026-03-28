namespace PSA.EntidadesDTO.DTOs.Pagos
{
    public class CuentaBancariaDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Banco { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public bool EsPrincipal { get; set; }
    }
}