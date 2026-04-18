namespace PSA.EntidadesDTO.DTOs.Pagos
{
    public class PlanPagoDTO
    {
        public int IdPlanPago { get; set; }
        public int IdFinca { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public int Anio { get; set; }
        public int IdConfiguracionPago { get; set; }
        public int? IdCuentaBancaria { get; set; }
        public decimal MontoBaseMensual { get; set; }
        public decimal PorcentajeAjusteTotal { get; set; }
        public decimal MontoMensualCalculado { get; set; }
        public string EstadoPlan { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public PlanPagoCalculoDetalleDTO? DetalleCalculo { get; set; }
    }

    public class PlanPagoCalculoDetalleDTO
    {
        public decimal HectareasAprobadas { get; set; }
        public decimal PrecioBasePorHectarea { get; set; }
        public decimal MontoBaseMensual { get; set; }
        public decimal PorcentajeVegetacion { get; set; }
        public decimal PorcentajeHidrico { get; set; }
        public decimal PorcentajeNacientes { get; set; }
        public decimal PorcentajePendiente { get; set; }
        public decimal PorcentajeTotalAntesTope { get; set; }
        public decimal PorcentajeTopeAplicado { get; set; }
        public decimal PorcentajeTotalAplicado { get; set; }
        public decimal MontoAjusteMensual { get; set; }
        public decimal MontoFinalMensual { get; set; }
        public string VegetacionFinal { get; set; } = string.Empty;
        public bool TieneRecursosHidricosFinal { get; set; }
        public int CantidadNacientesFinal { get; set; }
        public string PendienteFinal { get; set; } = string.Empty;
    }

    public class CuotaPlanPagoDTO
    {
        public int IdPlanPago { get; set; }
        public int IdCuotaPago { get; set; }
        public int IdFinca { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public int Anio { get; set; }
        public int Mes { get; set; }
        public DateTime FechaProgramada { get; set; }
        public decimal MontoProgramado { get; set; }
        public decimal MontoPendiente { get; set; }
        public string EstadoCuota { get; set; } = string.Empty;
        public DateTime? FechaPago { get; set; }
    }

    public class GenerarPlanPagoRequestDTO
    {
        public int IdFinca { get; set; }
        public int Anio { get; set; } = DateTime.UtcNow.Year + 1;
        public bool Simular { get; set; }
    }

    public class PlanPagoResumenDTO
    {
        public int IdPlanPago { get; set; }
        public int IdFinca { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public int Anio { get; set; }
        public decimal MontoMensualCalculado { get; set; }
        public decimal MontoAnualEstimado { get; set; }
        public string EstadoPlan { get; set; } = string.Empty;
        public int? IdCuentaBancaria { get; set; }
    }

    public class CuentaBancariaDuenoDTO
    {
        public int IdCuentaBancaria { get; set; }
        public string Banco { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public string EstadoValidacion { get; set; } = string.Empty;
        public bool Activa { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class RegistrarCuentaBancariaDTO
    {
        public int IdUsuario { get; set; }
        public string Banco { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
    }

    public class AsociarCuentaPlanDTO
    {
        public int IdUsuario { get; set; }
        public int IdCuentaBancaria { get; set; }
    }
}
