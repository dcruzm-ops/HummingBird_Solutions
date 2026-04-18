namespace PSA.EntidadesDTO.DTOs.Pagos
{
    public static class EstadosPlanPago
    {
        public const string BorradorGenerado = "BorradorGenerado";
        public const string PendienteDatosBancarios = "PendienteDatosBancarios";
        public const string PendienteAprobacionFinal = "PendienteAprobacionFinal";
        public const string Activo = "Activo";
        public const string Finalizado = "Finalizado";
        public const string Cancelado = "Cancelado";
    }

    public static class EstadosCuotaPago
    {
        public const string Pendiente = "Pendiente";
        public const string Ejecutada = "Ejecutada";
        public const string Notificada = "Notificada";
    }

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
        public decimal MontoAnualCalculado => Math.Round(MontoMensualCalculado * 12m, 2);
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

    public class AprobarPlanPagoFinalDTO
    {
        public int IdIngeniero { get; set; }
    }

    public class FiltroPlanesPagoDTO
    {
        public int? Anio { get; set; }
        public int? IdFinca { get; set; }
        public int? IdPropietario { get; set; }
        public int? IdIngeniero { get; set; }
        public string? EstadoPlan { get; set; }
        public bool SoloPendientes { get; set; }
    }

    public class OwnerPaymentPlanDto
    {
        public int IdPlanPago { get; set; }
        public int IdFinca { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string EstadoPlan { get; set; } = string.Empty;
        public int? IdCuentaBancaria { get; set; }
        public decimal MontoMensual { get; set; }
        public decimal MontoAnual { get; set; }
        public string EstadoCuentaBancaria { get; set; } = "Pendiente";
        public string? CuentaBancariaMascara { get; set; }
        public OwnerPaymentCalculationSummaryDto ResumenCalculo { get; set; } = new();
    }

    public class OwnerPaymentCalculationSummaryDto
    {
        public decimal HectareasAprobadas { get; set; }
        public string CoberturaVegetacion { get; set; } = string.Empty;
        public decimal AjusteAplicadoPorcentaje { get; set; }
        public decimal TopeAplicadoPorcentaje { get; set; }
        public bool SeAplicoTope { get; set; }
    }

    public class OwnerPaymentPlanDetailDto
    {
        public OwnerPaymentPlanDto Plan { get; set; } = new();
        public List<CuotaPlanPagoDTO> Cuotas { get; set; } = new();
    }

    public class EngineerPaymentImpactDto
    {
        public int IdEvaluacion { get; set; }
        public int IdFinca { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public string DecisionTecnica { get; set; } = string.Empty;
        public bool GeneroPlan { get; set; }
        public int? IdPlanPago { get; set; }
        public string EstadoPlan { get; set; } = string.Empty;
        public string EstadoContinuidad { get; set; } = string.Empty;
        public decimal? MontoMensualReferencial { get; set; }
        public decimal? MontoAnualReferencial { get; set; }
        public string EstadoCuentaBancaria { get; set; } = "Pendiente";
        public bool CuentaRegistrada { get; set; }
        public bool CuentaValidada { get; set; }
        public bool PuedeAprobarFinal => IdPlanPago.HasValue
            && string.Equals(EstadoPlan, EstadosPlanPago.PendienteAprobacionFinal, StringComparison.OrdinalIgnoreCase);
    }

    public class AdminPaymentPlanFilterDto
    {
        public int? Anio { get; set; }
        public int? IdFinca { get; set; }
        public int? IdPropietario { get; set; }
        public int? IdIngeniero { get; set; }
        public string? Provincia { get; set; }
        public string? Canton { get; set; }
        public string? Distrito { get; set; }
        public string? EstadoPlan { get; set; }
        public string? EstadoBancario { get; set; }
    }

    public class AdminPaymentPlanDto
    {
        public int IdPlanPago { get; set; }
        public int IdFinca { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public string Propietario { get; set; } = string.Empty;
        public int? IdIngeniero { get; set; }
        public string Ingeniero { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Canton { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string EstadoPlan { get; set; } = string.Empty;
        public string EstadoBancario { get; set; } = string.Empty;
        public string? CuentaBancariaMascara { get; set; }
        public decimal MontoMensual { get; set; }
        public decimal MontoAnual { get; set; }
        public int VersionConfiguracion { get; set; }
    }

    public class AdminPaymentPlanDetailDto
    {
        public AdminPaymentPlanDto Plan { get; set; } = new();
        public PlanPagoCalculoDetalleDTO Calculo { get; set; } = new();
        public List<CuotaPlanPagoDTO> Cuotas { get; set; } = new();
        public List<AuditoriaPlanPagoDto> Bitacora { get; set; } = new();
    }

    public class AuditoriaPlanPagoDto
    {
        public DateTime FechaAccion { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public int? IdUsuario { get; set; }
    }

    public class PlanPagoGenerationContextDTO
    {
        public int IdFinca { get; set; }
        public int IdEvaluacion { get; set; }
        public int IdPropietario { get; set; }
        public string NombreFinca { get; set; } = string.Empty;
        public decimal HectareasAprobadas { get; set; }
        public string VegetacionFinal { get; set; } = string.Empty;
        public bool TieneRecursosHidricosFinal { get; set; }
        public int CantidadNacientesFinal { get; set; }
        public string PendienteFinal { get; set; } = string.Empty;
    }

    public class PaymentConfigurationVersionDTO
    {
        public int IdConfiguracionPago { get; set; }
        public int Version { get; set; }
        public decimal PrecioBasePorHectarea { get; set; }
        public decimal TopePorcentajeAjuste { get; set; }
        public Dictionary<string, decimal> VegetacionAjustes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> HidricosAjustes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, decimal> PendienteAjustes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class PaymentCalculationResultDTO
    {
        public decimal MontoBaseMensual { get; set; }
        public decimal MontoAjusteMensual { get; set; }
        public decimal MontoMensualTotal { get; set; }
        public decimal MontoAnualTotal { get; set; }
        public decimal PorcentajeVegetacion { get; set; }
        public decimal PorcentajeHidrico { get; set; }
        public decimal PorcentajeNacientes { get; set; }
        public decimal PorcentajePendiente { get; set; }
        public decimal PorcentajeAjusteTotalBruto { get; set; }
        public decimal PorcentajeAjusteAplicado { get; set; }
        public decimal TopePorcentajeAjuste { get; set; }
    }
}
