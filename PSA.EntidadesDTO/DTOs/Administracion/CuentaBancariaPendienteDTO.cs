using System;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class CuentaBancariaPendienteDTO
    {
        public int IdCuentaBancaria { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public string EstadoValidacion { get; set; } = string.Empty;
        public string? ObservacionesValidacion { get; set; }
        public bool Activa { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
