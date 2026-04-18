using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.AppCore.Services;

public interface IPaymentPlanService
{
    Task<PlanPagoDTO?> GeneratePreliminaryPlanFromEvaluationAsync(int idEvaluacion, int anioPlan, int? actorId, string? ip);
    Task<bool> AttachBankAccountAsync(int idPlanPago, int idUsuario, int idCuentaBancaria, string? ip);
    Task<bool> ApproveFinalActivationAsync(int idPlanPago, int idIngeniero, string? ip);
}

public class PaymentPlanService(
    PlanPagoDAO planPagoDao,
    AuditoriaLogDAO auditoriaLogDao,
    IPaymentCalculationService paymentCalculationService) : IPaymentPlanService
{
    private readonly PlanPagoDAO _planPagoDao = planPagoDao;
    private readonly AuditoriaLogDAO _auditoriaLogDao = auditoriaLogDao;
    private readonly IPaymentCalculationService _paymentCalculationService = paymentCalculationService;

    public async Task<PlanPagoDTO?> GeneratePreliminaryPlanFromEvaluationAsync(int idEvaluacion, int anioPlan, int? actorId, string? ip)
    {
        var context = await _planPagoDao.ObtenerContextoGeneracionDesdeEvaluacionAsync(idEvaluacion);
        if (context is null)
        {
            return null;
        }

        var config = await _planPagoDao.ObtenerConfiguracionVigenteParaAnioAsync(anioPlan);
        if (config is null)
        {
            throw new InvalidOperationException($"No existe configuración de pago vigente para el año {anioPlan}.");
        }

        var calculation = _paymentCalculationService.Calculate(context, config);
        var plan = await _planPagoDao.CrearOActualizarPlanPreliminarAsync(context, config, calculation, anioPlan);

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: actorId,
            modulo: "Pagos",
            tablaAfectada: "PlanesPago",
            accion: "GENERAR_PLAN_PRELIMINAR",
            detalle: $"Plan preliminar #{plan.IdPlanPago} generado por evaluación #{idEvaluacion} para finca #{plan.IdFinca}.",
            idRegistroAfectado: plan.IdPlanPago,
            ipOrigen: ip);

        return plan;
    }

    public async Task<bool> AttachBankAccountAsync(int idPlanPago, int idUsuario, int idCuentaBancaria, string? ip)
    {
        var updated = await _planPagoDao.AsociarCuentaYPasarAPendienteAprobacionAsync(idPlanPago, idUsuario, idCuentaBancaria);
        if (!updated)
        {
            return false;
        }

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: idUsuario,
            modulo: "Pagos",
            tablaAfectada: "PlanesPago",
            accion: "ASOCIAR_CUENTA_PLAN",
            detalle: $"Cuenta bancaria #{idCuentaBancaria} asociada al plan #{idPlanPago}. Estado => {EstadosPlanPago.PendienteAprobacionFinal}.",
            idRegistroAfectado: idPlanPago,
            ipOrigen: ip);

        return true;
    }

    public async Task<bool> ApproveFinalActivationAsync(int idPlanPago, int idIngeniero, string? ip)
    {
        var activated = await _planPagoDao.AprobarPlanYActivarAsync(idPlanPago, idIngeniero);
        if (!activated)
        {
            return false;
        }

        await _auditoriaLogDao.RegistrarEventoAsync(
            idUsuario: idIngeniero,
            modulo: "Pagos",
            tablaAfectada: "PlanesPago",
            accion: "APROBAR_PLAN_FINAL",
            detalle: $"Plan de pago #{idPlanPago} aprobado y activado por ingeniero #{idIngeniero}.",
            idRegistroAfectado: idPlanPago,
            ipOrigen: ip);

        return true;
    }
}
